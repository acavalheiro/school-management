using System.Net;
using System.Net.Http.Json;
using Application.Common;
using AwesomeAssertions;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>
/// /api/auth/register and /api/auth/login. Rate-limited to 10 requests/minute per
/// remote IP (RateLimitPolicies.Auth) — a fresh WebAppFactory per test resets the
/// limiter, so keep each test method's own call count well under that.
/// </summary>
[TestFixture]
public sealed class AuthEndpointsTests
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();
        // Registration assigns AppRoles.Admin to the new user; RoleSeeder doesn't run
        // outside Development, so the Testing environment has no roles without this.
        await _factory.SeedRolesAsync(AppRoles.Admin);
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private Task<HttpResponseMessage> Register(string email, string password, string confirmPassword, string tenantName) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, password, confirmPassword, tenantName });

    private Task<HttpResponseMessage> Login(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    [Test]
    public async Task Register_ValidRequest_ReturnsOkWithUserId()
    {
        var response = await Register("admin@school.test", "Password123!", "Password123!", "Greenfield Academy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        body!.UserId.Should().NotBeEmpty();
    }

    [Test]
    public async Task Register_ThenLogin_SucceedsWithTheSameCredentials()
    {
        await Register("owner@school.test", "Password123!", "Password123!", "Greenfield Academy");

        var response = await Login("owner@school.test", "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<LoginResponse>();
        token!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Register_MismatchedConfirmPassword_ReturnsBadRequest()
    {
        var response = await Register("mismatch@school.test", "Password123!", "Different123!", "Greenfield Academy");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        await Register("dup@school.test", "Password123!", "Password123!", "First School");

        var response = await Register("dup@school.test", "Password123!", "Password123!", "Second School");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        await Register("login@school.test", "Password123!", "Password123!", "Greenfield Academy");

        var response = await Login("login@school.test", "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Register("wrongpass@school.test", "Password123!", "Password123!", "Greenfield Academy");

        var response = await Login("wrongpass@school.test", "NotThePassword1!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        // Same status as a wrong password — deliberately, so the endpoint can't be used
        // to enumerate registered emails. Assert status only, not body content.
        var response = await Login("nobody@nowhere.test", "Whatever123!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record RegisterResponse(Guid UserId);
    private sealed record LoginResponse(string Token, DateTime ExpiresAt);
}
