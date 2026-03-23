using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Queries;

public sealed class ListUsersQueryHandler(IIdentityService identityService, ITenantService tenantService)
    : IRequestHandler<ListUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    public Task<Result<IReadOnlyList<UserDto>>> Handle(ListUsersQuery request, CancellationToken cancellationToken) =>
        identityService.ListUsersAsync(tenantService.TenantId, cancellationToken);
}
