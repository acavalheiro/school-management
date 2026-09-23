using Application.Auth.Queries;
using Application.Users.Queries;
using Domain.Common;

namespace Application.Common.Interfaces;

/// <summary>
/// Identity operations behind ASP.NET Core Identity — registration, login, and
/// tenant-scoped user management. Implemented by <c>IdentityService</c>, which
/// filters Identity's unfiltered tables explicitly since they carry no query filter
/// of their own.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates the tenant's first user (Admin) as part of self-service registration.</summary>
    Task<Result<Guid>> RegisterAsync(string email, string password, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Verifies credentials via <c>SignInManager</c> (not <c>UserManager.CheckPasswordAsync</c>), so lockout is enforced.</summary>
    Task<Result<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Lists users of the given tenant. Admin only.</summary>
    Task<Result<IReadOnlyList<UserDto>>> ListUsersAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// SuperAdmin provisioning: creates a user in an explicit tenant with an explicit
    /// role and a generated temporary password. Unlike <see cref="RegisterAsync"/>, the
    /// caller is a trusted authenticated SuperAdmin, so failures are reported specifically.
    /// </summary>
    Task<Result<CreatedUserDto>> CreateUserAsync(string email, Guid tenantId, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Reassigns the target user's role. Verifies <paramref name="callerTenantId"/> matches
    /// the target user's tenant, and adds the new role before removing old ones so a failed
    /// add cannot strip a user of every role.
    /// </summary>
    Task<Result> UpdateUserRoleAsync(Guid userId, string role, Guid callerTenantId, CancellationToken cancellationToken);

    /// <summary>Deletes the target user after verifying <paramref name="callerTenantId"/> matches their tenant.</summary>
    Task<Result> DeleteUserAsync(Guid userId, Guid callerTenantId, CancellationToken cancellationToken);

    /// <summary>Deletes every Identity user belonging to a tenant, as part of tenant deletion.</summary>
    Task<Result> DeleteTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken);
}
