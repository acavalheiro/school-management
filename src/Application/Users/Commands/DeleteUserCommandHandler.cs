using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Commands;

public sealed class DeleteUserCommandHandler(IIdentityService identityService, ITenantService tenantService)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken) =>
        identityService.DeleteUserAsync(request.UserId, tenantService.TenantId, cancellationToken);
}
