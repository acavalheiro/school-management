using Application.Auth.Queries;
using Application.Users.Queries;
using Domain.Common;

namespace Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Guid>> RegisterAsync(string email, string password, Guid tenantId, CancellationToken cancellationToken);
    Task<Result<AuthTokenDto>> LoginAsync(string email, string password, CancellationToken cancellationToken);

    // User management (Admin only)
    Task<Result<IReadOnlyList<UserDto>>> ListUsersAsync(Guid tenantId, CancellationToken cancellationToken);

    // SuperAdmin provisioning: creates a user in an explicit tenant with an explicit
    // role and a generated temporary password. Unlike RegisterAsync, the caller is a
    // trusted authenticated SuperAdmin, so failures are reported specifically.
    Task<Result<CreatedUserDto>> CreateUserAsync(string email, Guid tenantId, string role, CancellationToken cancellationToken);
    Task<Result> UpdateUserRoleAsync(Guid userId, string role, Guid callerTenantId, CancellationToken cancellationToken);
    Task<Result> DeleteUserAsync(Guid userId, Guid callerTenantId, CancellationToken cancellationToken);
    Task<Result> DeleteTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken);
}
