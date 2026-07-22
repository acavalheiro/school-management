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
using Microsoft.Extensions.Configuration;
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

    // Startup refuses to run without a valid signing key, and user-secrets are not
    // loaded outside Development. Program validates during CreateBuilder, before any
    // web-host configuration callback runs, so this has to arrive as an environment
    // variable. The value is test-only and signs nothing real.
    static WebAppFactory()
    {
        Environment.SetEnvironmentVariable(
            "JwtSettings__Secret", "integration-tests-signing-key-not-used-in-any-real-environment");
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "atl-api-tests");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "atl-ui-tests");
    }

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

            // ITenantService is deliberately NOT replaced: tests exercise the real
            // claim-based resolution, driven per request by TestAuthHandler.

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
    public HttpClient CreateClientFor(Guid tenantId, string role = AppRoles.Admin) =>
        CreateClientWithTenantHeader(tenantId.ToString(), role);

    /// <summary>
    /// Creates an authenticated client whose token carries no `tid` claim — the
    /// malformed-token case that must fail closed.
    /// </summary>
    public HttpClient CreateClientWithoutTenant(string role = AppRoles.Admin) =>
        CreateClientWithTenantHeader(TestAuthHandler.NoTenant, role);

    private HttpClient CreateClientWithTenantHeader(string tenantHeader, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantHeader, tenantHeader);
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

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string TenantHeader = "X-Test-Tenant";
    public const string RoleHeader = "X-Test-Role";
    public const string NoTenant = "none";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers.TryGetValue(RoleHeader, out var roleHeader)
            ? roleHeader.ToString()
            : AppRoles.Admin;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Role, role)
        };

        var tenantHeaderValue = Request.Headers.TryGetValue(TenantHeader, out var tenantHeader)
            ? tenantHeader.ToString()
            : WebAppFactory.TestTenantId.ToString();

        // NoTenant emits a token with no `tid` at all, so tests can assert that an
        // unresolvable tenant fails closed rather than exposing every school.
        if (tenantHeaderValue != NoTenant)
            claims.Add(new Claim("tid", tenantHeaderValue));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
