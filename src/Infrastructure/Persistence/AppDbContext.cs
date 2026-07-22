using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options),
      IAppDbContext
{
    // Both are set by the IAppDbContext scoped factory before being returned to callers.

    // Guid.Empty means no tenant context (anonymous requests, migrations, seeder).
    // It matches no rows — it does NOT disable the filter.
    public Guid TenantId { get; set; } = Guid.Empty;

    // The only way to read across tenants. SuperAdmin only; defaults to closed so
    // that migrations, the seeder, and any unconfigured caller stay scoped.
    public bool BypassTenantFilter { get; set; }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Student>()
            .HasQueryFilter(s =>
                BypassTenantFilter ||
                s.TenantId == TenantId);
    }
}
