using Application.Auth.Queries;
using Application.Common;
using Application.Common.Interfaces;
using Application.Users.Queries;
using Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    ILogger<IdentityService> logger)
    : IIdentityService
{
    public async Task<Result<Guid>> RegisterAsync(
        string email,
        string password,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        // Registration failures are reported generically. Echoing Identity's
        // "email already taken" to an anonymous caller turns this endpoint into a
        // membership oracle for every parent and staff address in the system.
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogInformation("Registration attempted for an existing email");
            return RegistrationFailed;
        }

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
            logger.LogWarning(
                "Registration failed: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return RegistrationFailed;
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
            return InvalidCredentials;

        // CheckPasswordSignInAsync (not UserManager.CheckPasswordAsync) is what
        // increments the failed-attempt counter and enforces the lockout window.
        var signIn = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            // Deliberately reported as invalid credentials: telling an anonymous
            // caller that an account exists and is locked is an enumeration oracle.
            logger.LogWarning("Login blocked for locked-out user {UserId}", user.Id);
            return InvalidCredentials;
        }

        if (!signIn.Succeeded)
            return InvalidCredentials;

        var roles = await userManager.GetRolesAsync(user);
        return tokenService.GenerateToken(
            user.Id, user.Email!, roles, user.TenantId, await userManager.GetSecurityStampAsync(user));
    }

    private static Error InvalidCredentials =>
        new("Auth.InvalidCredentials", "Invalid email or password.");

    private static Error RegistrationFailed =>
        new("Auth.RegistrationFailed", "Registration could not be completed.");

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

        // Tokens already issued carry the old role. Rotating the stamp revokes them
        // so a demotion takes effect immediately rather than at token expiry.
        await userManager.UpdateSecurityStampAsync(user);

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
