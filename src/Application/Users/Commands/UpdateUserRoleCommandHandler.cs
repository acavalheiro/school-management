using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Commands;

public sealed class UpdateUserRoleCommandHandler(IIdentityService identityService, ITenantService tenantService)
    : IRequestHandler<UpdateUserRoleCommand, Result>
{
    public Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken) =>
        identityService.UpdateUserRoleAsync(request.UserId, request.Role, tenantService.TenantId, cancellationToken);
}
