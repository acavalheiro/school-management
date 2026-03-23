using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Commands;

public sealed class UpdateTenantCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateTenantCommand, Result>
{
    public async Task<Result> Handle(
        UpdateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Error.NotFound(nameof(Tenant), request.TenantId);

        var renameResult = tenant.Rename(request.Name);
        if (renameResult.IsFailure)
            return renameResult;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
