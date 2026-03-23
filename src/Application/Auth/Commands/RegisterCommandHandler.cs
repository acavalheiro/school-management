using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;

namespace Application.Auth.Commands;

public sealed class RegisterCommandHandler(IIdentityService identityService, IAppDbContext db)
    : IRequestHandler<RegisterCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var tenantResult = Tenant.Create(request.TenantName);
        if (tenantResult.IsFailure)
            return tenantResult.Error;

        var tenant = tenantResult.Value!;
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        return await identityService.RegisterAsync(request.Email, request.Password, tenant.Id, cancellationToken);
    }
}
