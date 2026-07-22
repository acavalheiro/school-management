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

        // The endpoint is anonymous, so a failure after the tenant is saved would let
        // anyone accumulate orphaned Tenant rows by re-registering a taken email.
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        var registerResult = await identityService.RegisterAsync(
            request.Email, request.Password, tenant.Id, cancellationToken);

        if (registerResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return registerResult.Error;
        }

        await transaction.CommitAsync(cancellationToken);
        return registerResult;
    }
}
