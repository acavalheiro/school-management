using Application.Auth.Queries;
using Application.Common;
using Application.Common.Interfaces;
using Application.Users.Queries;
using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    TokenService tokenService)
    : IIdentityService
{
    public async Task<Result<Guid>> RegisterAsync(
        string email,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Error.Conflict(nameof(ApplicationUser));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            TenantId = tenantId
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var description = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return new Error("Identity.Register", description);
        }

        await userManager.AddToRoleAsync(user, AppRoles.Admin);

        return user.Id;
    }

    public async Task<Result<AuthTokenDto>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return new Error("Auth.InvalidCredentials", "Invalid email or password.");

        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
            return new Error("Auth.InvalidCredentials", "Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        return tokenService.GenerateToken(user.Id, user.Email!, roles, user.TenantId);
    }

    public async Task<Result<IReadOnlyList<UserDto>>> ListUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var users = userManager.Users.Where(u => u.TenantId == tenantId).ToList();

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtos.Add(new UserDto(user.Id, user.Email!, roles.FirstOrDefault() ?? AppRoles.User));
        }

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    public async Task<Result> UpdateUserRoleAsync(Guid userId, string role, Guid callerTenantId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.TenantId != callerTenantId)
            return Error.NotFound(nameof(ApplicationUser), userId);

        // Defence in depth: never let a tenant-scoped caller assign SuperAdmin,
        // regardless of what validation ran upstream.
        if (!AppRoles.IsAssignable(role))
            return Error.Validation(nameof(role), "Role is not assignable.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count == 1 && currentRoles[0] == role)
            return Result.Success();

        // Add before removing: a failed add must leave the user's existing roles
        // intact rather than stripping them and locking the account out.
        var addResult = await userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            var description = string.Join("; ", addResult.Errors.Select(e => e.Description));
            return new Error("Identity.UpdateRole", description);
        }

        var staleRoles = currentRoles.Where(r => r != role).ToList();
        if (staleRoles.Count != 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, staleRoles);
            if (!removeResult.Succeeded)
            {
                var description = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                return new Error("Identity.UpdateRole", description);
            }
        }

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(Guid userId, Guid callerTenantId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.TenantId != callerTenantId)
            return Error.NotFound(nameof(ApplicationUser), userId);

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var description = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
            return new Error("Identity.Delete", description);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var users = userManager.Users.Where(u => u.TenantId == tenantId).ToList();

        foreach (var user in users)
        {
            var deleteResult = await userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var description = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                return new Error("Identity.DeleteTenantUsers", description);
            }
        }

        return Result.Success();
    }
}
