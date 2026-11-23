using System.Net;
using System.Net.Http.Json;
using Application.Common;
using AwesomeAssertions;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>
/// <c>/api/me</c> acts on the account in the caller's token. Identity tables carry no tenant
/// filter, so these tests are the isolation boundary: one user must never read or change another.
/// </summary>
[TestFixture]
public sealed class MeEndpointsTests
{
    // Changes always move a user *away* from the default, so "others are untouched" can't pass
    // vacuously just because the value written happens to equal the default.
    private static readonly string NonDefault =
        SupportedLanguages.All.First(l => l != SupportedLanguages.Default);

    private WebAppFactory _factory = null!;
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    [SetUp]
    public void SetUp() => _factory = new WebAppFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task GetMe_NewUser_ReturnsDefaultLanguage()
    {
        var userId = await _factory.SeedUserAsync("a@school-a.test", _tenantA, AppRoles.User);
        using var client = _factory.CreateClientForUser(userId, _tenantA, AppRoles.User);

        var me = await client.GetFromJsonAsync<MeResponse>("/api/me");

        me!.Id.Should().Be(userId);
        me.Email.Should().Be("a@school-a.test");
        me.Role.Should().Be(AppRoles.User);
        me.PreferredLanguage.Should().Be(SupportedLanguages.PtPt, "pt-PT is the default for new accounts");
    }

    [Test]
    public async Task GetMe_UnknownUser_ReturnsNotFound()
    {
        // No X-Test-User header: the token's `sub` is a random id with no account behind it.
        using var client = _factory.CreateClientFor(_tenantA);

        var response = await client.GetAsync("/api/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestCase(AppRoles.User)]
    [TestCase(AppRoles.Admin)]
    [TestCase(AppRoles.SuperAdmin)]
    public async Task UpdateLanguage_AnyRole_PersistsAndIsReturnedByGetMe(string role)
    {
        var userId = await _factory.SeedUserAsync($"{role}@school-a.test", _tenantA, role);
        using var client = _factory.CreateClientForUser(userId, _tenantA, role);

        var put = await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });
        var me = await client.GetFromJsonAsync<MeResponse>("/api/me");

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);
        me!.PreferredLanguage.Should().Be(NonDefault);
    }

    [TestCase("pt-BR")]
    [TestCase("en")]
    [TestCase("PT-pt")]
    [TestCase("")]
    public async Task UpdateLanguage_UnsupportedLanguage_ReturnsBadRequestAndLeavesItUnchanged(string language)
    {
        var userId = await _factory.SeedUserAsync("a@school-a.test", _tenantA);
        using var client = _factory.CreateClientForUser(userId, _tenantA);

        var response = await client.PutAsJsonAsync("/api/me/language", new { language });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _factory.FindUserAsync(userId)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
    }

    [Test]
    public async Task UpdateLanguage_ChangesOnlyTheCallersOwnAccount()
    {
        var userA = await _factory.SeedUserAsync("a@school-a.test", _tenantA);
        var userB = await _factory.SeedUserAsync("b@school-b.test", _tenantB);
        var sameTenantUser = await _factory.SeedUserAsync("a2@school-a.test", _tenantA);
        using var clientA = _factory.CreateClientForUser(userA, _tenantA);

        var response = await clientA.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _factory.FindUserAsync(userA)).PreferredLanguage.Should().Be(NonDefault);
        (await _factory.FindUserAsync(userB)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
        (await _factory.FindUserAsync(sameTenantUser)).PreferredLanguage.Should().Be(SupportedLanguages.Default);
    }

    [Test]
    public async Task UpdateLanguage_DoesNotRotateTheSecurityStamp()
    {
        var userId = await _factory.SeedUserAsync("a@school-a.test", _tenantA);
        var stampBefore = (await _factory.FindUserAsync(userId)).SecurityStamp;
        using var client = _factory.CreateClientForUser(userId, _tenantA);

        await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        // A rotated stamp would revoke every token the user holds: switching language must not sign them out.
        (await _factory.FindUserAsync(userId)).SecurityStamp.Should().Be(stampBefore);
    }

    [Test]
    public async Task UpdateLanguage_UnknownUser_ReturnsNotFound()
    {
        using var client = _factory.CreateClientFor(_tenantA);

        var response = await client.PutAsJsonAsync("/api/me/language", new { language = NonDefault });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record MeResponse(Guid Id, string Email, string Role, string PreferredLanguage);
}
