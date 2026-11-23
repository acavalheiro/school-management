using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Me.Commands;

public sealed class UpdateMyLanguageCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    : IRequestHandler<UpdateMyLanguageCommand, Result>
{
    public Task<Result> Handle(UpdateMyLanguageCommand request, CancellationToken cancellationToken) =>
        identityService.UpdatePreferredLanguageAsync(currentUser.UserId, request.Language, cancellationToken);
}
