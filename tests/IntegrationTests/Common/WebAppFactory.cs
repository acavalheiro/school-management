using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Common;

public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    public static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations to avoid provider conflicts.
            // EF Core 10 accumulates IDbContextOptionsConfiguration<T> per AddDbContext call,
            // so we must remove those too — otherwise Npgsql and InMemory both get applied.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1" &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext)))
                .ToList();

            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Use in-memory database for integration tests
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"IntegrationTest_{Guid.NewGuid()}"));

            // Replace TenantService with a fixed test tenant
            var tenantDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantService));

            if (tenantDescriptor is not null)
                services.Remove(tenantDescriptor);

            services.AddSingleton<ITenantService>(new TestTenantService(TestTenantId));

            // Override authentication to auto-authenticate all test requests
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });

        builder.UseEnvironment("Testing");
    }
}

internal sealed class TestTenantService(Guid tenantId) : ITenantService
{
    public Guid TenantId => tenantId;
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, AppRoles.Admin)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
