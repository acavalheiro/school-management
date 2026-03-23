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
    Task<Result> UpdateUserRoleAsync(Guid userId, string role, Guid callerTenantId, CancellationToken cancellationToken);
    Task<Result> DeleteUserAsync(Guid userId, Guid callerTenantId, CancellationToken cancellationToken);
    Task<Result> DeleteTenantUsersAsync(Guid tenantId, CancellationToken cancellationToken);
}
