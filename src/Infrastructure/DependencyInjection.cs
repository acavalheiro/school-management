using Application.Common.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(opts =>
            configuration.GetSection(JwtSettings.SectionName).Bind(opts));

        services.Configure<AdminSettings>(opts =>
            configuration.GetSection(AdminSettings.SectionName).Bind(opts));

        services.Configure<SuperAdminSettings>(opts =>
            configuration.GetSection(SuperAdminSettings.SectionName).Bind(opts));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var tenantService = sp.GetRequiredService<ITenantService>();
            db.TenantId = tenantService.TenantId;
            db.BypassTenantFilter = tenantService.CanBypassTenantFilter;
            return db;
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<RoleSeeder>();

        return services;
    }
}
