using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;

namespace Application.Tenants.Commands;

public sealed class CreateTenantCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var result = Tenant.Create(request.Name);
        if (result.IsFailure)
            return result.Error;

        db.Tenants.Add(result.Value!);
        await db.SaveChangesAsync(cancellationToken);

        return result.Value!.Id;
    }
}
