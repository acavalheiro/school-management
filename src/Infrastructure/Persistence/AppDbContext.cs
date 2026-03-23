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
    // Set by the IAppDbContext scoped factory before being returned to callers.
    // Guid.Empty means no tenant context (migrations / seeder) — filter is bypassed.
    public Guid TenantId { get; set; } = Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Student>()
            .HasQueryFilter(s =>
                TenantId == Guid.Empty ||
                s.TenantId == TenantId);
    }
}
