using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Application.Common;
using AwesomeAssertions;
using IntegrationTests.Common;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationTests.Endpoints;

/// <summary>
/// <c>/api/me</c> through the production JWT pipeline rather than <see cref="TestAuthHandler"/>.
/// The caller's identity comes only from the token's <c>sub</c>, so these tests prove what the
/// test handler cannot: that a real token resolves to its owner, that tokens which are missing,
/// forged or revoked are rejected, and that changing language does not revoke the token in use.
/// </summary>
[TestFixture]
public sealed class MeEndpointsRealJwtTests
{
    // Changes always move a user *away* from the default, so "others are untouched" can't pass
    // vacuously just because the value written happens to equal the default.
    private static readonly string NonDefault =
        SupportedLanguages.All.First(l => l != SupportedLanguages.Default);

    private WebAppFactory _factory = null!;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    [SetUp]
    public void SetUp() => _factory = new WebAppFactory(useRealAuth: true);

    [TearDown]
    public void TearDown() => _factory.Dispose();

    private async Task<string> LoginAsync(string email)
    {
        using var anonymous = _factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync(
            "/api/auth/login", new { email, password = WebAppFactory.SeededPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK, "seeded users must be able to sign in");
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return body!.Token;
    }

    private HttpClient ClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string MintToken(Guid userId, string signingKey, string securityStamp)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: WebAppFactory.JwtIssuer,
            audience: WebAppFactory.JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("sstamp", securityStamp),
                new Claim(ClaimTypes.Role, AppRoles.Admin),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Test]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateLanguage_WithoutToken_ReturnsUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetMe_WithRealToken_ReturnsTheTokensOwner()
    {
        var userId = await _factory.SeedUserAsync("owner@school-a.test", _tenantA, AppRoles.User);
        await _factory.SeedUserAsync("other@school-a.test", _tenantA, AppRoles.Admin);
        using var client = ClientWithToken(await LoginAsync("owner@school-a.test"));

        var me = await client.GetFromJsonAsync<MeResponse>("/api/me");

        me!.Id.Should().Be(userId);
        me.Email.Should().Be("owner@school-a.test");
        me.Role.Should().Be(AppRoles.User);
    }

    [Test]
    public async Task UpdateLanguage_WithRealToken_ChangesOnlyTheTokensOwnerAndKeepsTheTokenValid()
    {
        var userA = await _factory.SeedUserAsync("a@school-a.test", _tenantA);
        var userB = await _factory.SeedUserAsync("b@school-b.test", _tenantB);
        using var client = ClientWithToken(await LoginAsync("a@school-a.test"));

        var put = await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });
        var after = await client.GetAsync("/api/me");

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // The same token still works: a language change must not rotate the security stamp.
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        (await after.Content.ReadFromJsonAsync<MeResponse>())!.PreferredLanguage.Should().Be(NonDefault);
        (await _factory.FindUserAsync(userA)).PreferredLanguage.Should().Be(NonDefault);
        (await _factory.FindUserAsync(userB)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
    }

    [Test]
    public async Task UpdateLanguage_TokenForgedWithAnotherKey_IsRejectedAndChangesNothing()
    {
        var victim = await _factory.SeedUserAsync("victim@school-b.test", _tenantB);
        var stamp = (await _factory.FindUserAsync(victim)).SecurityStamp!;
        var forged = MintToken(victim, "an-attacker-chosen-key-that-is-long-enough-for-hs256", stamp);
        using var client = ClientWithToken(forged);

        var response = await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _factory.FindUserAsync(victim)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
    }

    [Test]
    public async Task UpdateLanguage_ValidlySignedTokenWithStaleStamp_IsRejectedAndChangesNothing()
    {
        var victim = await _factory.SeedUserAsync("victim@school-b.test", _tenantB);
        var stale = MintToken(victim, WebAppFactory.JwtSecret, securityStamp: "not-the-current-stamp");
        using var client = ClientWithToken(stale);

        var response = await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _factory.FindUserAsync(victim)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
    }

    private sealed record TokenResponse(string Token, DateTime ExpiresAt);

    private sealed record MeResponse(Guid Id, string Email, string Role, string PreferredLanguage);
}
