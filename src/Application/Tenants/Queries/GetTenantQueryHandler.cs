using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Queries;

public sealed class GetTenantQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(
        GetTenantQuery request,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Error.NotFound(nameof(Tenant), request.TenantId);

        return new TenantDto(tenant.Id, tenant.Name, tenant.CreatedAt);
    }
}
