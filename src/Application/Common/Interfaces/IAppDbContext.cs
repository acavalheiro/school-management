using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common.Interfaces;

/// <summary>
/// Tenant-scoped view of the database. Resolved as a scoped factory that stamps
/// <c>TenantId</c> and <c>BypassTenantFilter</c> from <see cref="ITenantService"/>
/// before handing out the context, so every handler that takes this interface
/// (rather than <c>AppDbContext</c> directly) sees a correctly filtered view.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Globally visible — has no tenant query filter. SuperAdmin-only endpoints only.</summary>
    DbSet<Tenant> Tenants { get; }

    /// <summary>Filtered by <c>BypassTenantFilter || s.TenantId == TenantId</c>; fails closed on an unresolved tenant.</summary>
    DbSet<Student> Students { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Identity uses this same DbContext, so a transaction started here also covers
    /// UserManager writes — letting registration create a tenant and its user
    /// atomically.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
