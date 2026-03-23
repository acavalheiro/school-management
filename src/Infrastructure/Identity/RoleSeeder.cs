using Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public sealed class RoleSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSettings> adminOptions,
    IOptions<SuperAdminSettings> superAdminOptions,
    ILogger<RoleSeeder> logger)
{
    public async Task SeedAsync()
    {
        await EnsureRoleAsync(AppRoles.SuperAdmin);
        await EnsureRoleAsync(AppRoles.Admin);
        await EnsureRoleAsync(AppRoles.User);
        await EnsureDefaultSuperAdminAsync();
        await EnsureDefaultAdminAsync();
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            if (result.Succeeded)
                logger.LogInformation("Created role '{Role}'", roleName);
        }
    }

    private async Task EnsureDefaultSuperAdminAsync()
    {
        var settings = superAdminOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.Email))
            return;

        var existing = await userManager.FindByEmailAsync(settings.Email);
        if (existing is not null)
            return;

        var superAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = settings.Email,
            Email = settings.Email,
            EmailConfirmed = true,
            TenantId = Guid.Empty   // SuperAdmin belongs to no tenant
        };

        var createResult = await userManager.CreateAsync(superAdmin, settings.Password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create default super admin: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
        logger.LogInformation("Default super admin '{Email}' created", settings.Email);
    }

    private async Task EnsureDefaultAdminAsync()
    {
        var settings = adminOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.Email))
            return;

        var existing = await userManager.FindByEmailAsync(settings.Email);
        if (existing is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = settings.Email,
            Email = settings.Email,
            EmailConfirmed = true,
            TenantId = Guid.TryParse(settings.TenantId, out var tid) ? tid : Guid.NewGuid()
        };

        var createResult = await userManager.CreateAsync(admin, settings.Password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Failed to create default admin: {Errors}",
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Default admin '{Email}' created", settings.Email);
    }
}
