using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common;
using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Common;

public sealed class WebAppFactory : WebApplicationFactory<Program>
{
    public static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // Must be computed once per factory: generating it inside the options lambda
    // gives every scope its own database, so seeded data is invisible to requests.
    private readonly string _databaseName = $"IntegrationTest_{Guid.NewGuid()}";

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
                options.UseInMemoryDatabase(_databaseName));

            // Resolve the tenant from the request's `tid` claim, exactly as the real
            // TenantService does. A fixed stub here would make cross-tenant leaks
            // untestable, so tests drive the tenant per request instead.
            var tenantDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITenantService));

            if (tenantDescriptor is not null)
                services.Remove(tenantDescriptor);

            services.AddScoped<ITenantService, ClaimsTestTenantService>();

            // Override authentication to auto-authenticate all test requests
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates a client whose requests are authenticated as <paramref name="role"/>
    /// within <paramref name="tenantId"/>.
    /// </summary>
    public HttpClient CreateClientFor(Guid tenantId, string role = AppRoles.Admin)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, tenantId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    /// <summary>
    /// Writes directly to the database with the tenant filter bypassed, so tests can
    /// arrange data belonging to tenants other than the caller's.
    /// </summary>
    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }
}

/// <summary>Mirrors the production TenantService: reads the `tid` claim off the request.</summary>
internal sealed class ClaimsTestTenantService(IHttpContextAccessor httpContextAccessor) : ITenantService
{
    public Guid TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("tid");
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string TenantHeader = "X-Test-Tenant";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var tenantId = Request.Headers.TryGetValue(TenantHeader, out var tenantHeader)
                       && Guid.TryParse(tenantHeader, out var parsed)
            ? parsed
            : WebAppFactory.TestTenantId;

        var role = Request.Headers.TryGetValue(RoleHeader, out var roleHeader)
            ? roleHeader.ToString()
            : AppRoles.Admin;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, role),
            new Claim("tid", tenantId.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
