using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Queries;

public sealed class ListTenantUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<ListTenantUsersQuery, Result<IReadOnlyList<UserDto>>>
{
    public Task<Result<IReadOnlyList<UserDto>>> Handle(ListTenantUsersQuery request, CancellationToken cancellationToken) =>
        identityService.ListUsersAsync(request.TenantId, cancellationToken);
}
