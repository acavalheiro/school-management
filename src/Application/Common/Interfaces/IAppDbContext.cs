using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Student> Students { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Identity uses this same DbContext, so a transaction started here also covers
    /// UserManager writes — letting registration create a tenant and its user
    /// atomically.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
