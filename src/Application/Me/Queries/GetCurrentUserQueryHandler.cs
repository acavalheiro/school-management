using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Me.Queries;

public sealed class GetCurrentUserQueryHandler(IIdentityService identityService, ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken) =>
        identityService.GetCurrentUserAsync(currentUser.UserId, cancellationToken);
}
