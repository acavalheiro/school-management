using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Queries;

public sealed class ListTenantsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListTenantsQuery, Result<IReadOnlyList<TenantDto>>>
{
    public async Task<Result<IReadOnlyList<TenantDto>>> Handle(
        ListTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await db.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(t.Id, t.Name, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TenantDto>>.Success(tenants);
    }
}
