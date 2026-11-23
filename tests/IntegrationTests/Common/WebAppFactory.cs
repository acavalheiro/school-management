using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Common;
using Application.Common.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Common;

/// <param name="useRealAuth">
/// When true, keeps the production JWT bearer pipeline (signature, issuer, audience, lifetime and
/// security-stamp checks) instead of installing <see cref="TestAuthHandler"/>. Requests then need a
/// real token from <c>/api/auth/login</c>, and anonymous requests get 401 as in production.
/// </param>
public sealed class WebAppFactory(bool useRealAuth = false) : WebApplicationFactory<Program>
{
    /// <summary>Must match the JwtSettings environment variables set in the static constructor.</summary>
    public const string JwtSecret = "integration-tests-signing-key-not-used-in-any-real-environment";
    public const string JwtIssuer = "atl-api-tests";
    public const string JwtAudience = "atl-ui-tests";

    /// <summary>The password <see cref="SeedUserAsync"/> gives every seeded account.</summary>
    public const string SeededPassword = "Seeded-Password-1";

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
        Environment.SetEnvironmentVariable("JwtSettings__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("JwtSettings__Audience", JwtAudience);
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

            // Use in-memory database for integration tests. InMemory has no
            // transactions and escalates that to an error by default; ignore the
            // warning so handlers that open a transaction (registration, user
            // provisioning) run — the transaction is simply a no-op here, so tests
            // must not depend on rollback actually reverting a write.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            // ITenantService is deliberately NOT replaced: tests exercise the real
            // claim-based resolution, driven per request by TestAuthHandler.

            if (useRealAuth)
                return;

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
    /// Creates a client authenticated as a specific user: its token's <c>sub</c> is
    /// <paramref name="userId"/>. Needed for <c>/api/me</c>, which acts on the caller's own account.
    /// </summary>
    public HttpClient CreateClientForUser(Guid userId, Guid tenantId, string role = AppRoles.Admin)
    {
        var client = CreateClientWithTenantHeader(tenantId.ToString(), role);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        return client;
    }

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

    /// <summary>
    /// Creates a real Identity user (with its role) in <paramref name="tenantId"/> and returns its id.
    /// Pair with <see cref="CreateClientForUser"/> to call the API as that user.
    /// </summary>
    public async Task<Guid> SeedUserAsync(string email, Guid tenantId, string role = AppRoles.Admin)
    {
        await SeedRolesAsync(role);

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, TenantId = tenantId };

        var created = await userManager.CreateAsync(user, SeededPassword);
        if (!created.Succeeded)
            throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, role);

        return user.Id;
    }

    /// <summary>Reads a user straight from Identity, bypassing the API, so tests can assert on stored state.</summary>
    public async Task<ApplicationUser> FindUserAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");
    }

    /// <summary>
    /// Seeds Identity roles. RoleSeeder only runs in Development, so the Testing
    /// environment has none — yet AddToRoleAsync (used when provisioning users) needs
    /// the target role to exist. Call this before exercising user-creation endpoints.
    /// </summary>
    public async Task SeedRolesAsync(params string[] roles)
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
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
    public const string UserHeader = "X-Test-User";
    public const string NoTenant = "none";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers.TryGetValue(RoleHeader, out var roleHeader)
            ? roleHeader.ToString()
            : AppRoles.Admin;

        // `sub` mirrors the real JWT (MapInboundClaims is off). Without X-Test-User each
        // request is a fresh, unknown user, so /api/me returns 404 rather than someone's data.
        var userId = Request.Headers.TryGetValue(UserHeader, out var userHeader)
            ? userHeader.ToString()
            : Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
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
