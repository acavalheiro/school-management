using System.Net;
using System.Net.Http.Json;
using Application.Common;
using AwesomeAssertions;
using Domain.Entities;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>SuperAdmin-only tenant lifecycle: /api/tenants CRUD.</summary>
[TestFixture]
public sealed class TenantEndpointsTests
{
    private WebAppFactory _factory = null!;
    private Guid _tenantId;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();

        await _factory.SeedAsync(db =>
        {
            var tenant = Tenant.Create("Greenfield Academy").Value!;
            _tenantId = tenant.Id;
            db.Tenants.Add(tenant);
            return Task.CompletedTask;
        });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task ListTenants_AsSuperAdmin_ReturnsAllTenants()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var tenants = await superAdmin.GetFromJsonAsync<List<TenantResponse>>("/api/tenants");

        tenants.Should().ContainSingle(t => t.Id == _tenantId && t.Name == "Greenfield Academy");
    }

    [Test]
    public async Task ListTenants_AsAdmin_IsForbidden()
    {
        using var admin = _factory.CreateClientFor(_tenantId, AppRoles.Admin);

        var response = await admin.GetAsync("/api/tenants");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetTenant_ExistingId_ReturnsTenant()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var tenant = await superAdmin.GetFromJsonAsync<TenantResponse>($"/api/tenants/{_tenantId}");

        tenant!.Name.Should().Be("Greenfield Academy");
    }

    [Test]
    public async Task GetTenant_UnknownId_ReturnsNotFound()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.GetAsync($"/api/tenants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateTenant_ValidName_ReturnsCreatedWithLocationAndTenantId()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/tenants", new { name = "New Academy" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = await response.Content.ReadFromJsonAsync<CreateTenantResponse>();
        created!.TenantId.Should().NotBeEmpty();
    }

    [Test]
    public async Task CreateTenant_WhitespaceName_ReturnsBadRequest()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/tenants", new { name = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTenant_ExistingTenant_ReturnsNoContentAndRenamePersists()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PutAsJsonAsync($"/api/tenants/{_tenantId}", new { name = "Renamed Academy" });
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var tenant = await superAdmin.GetFromJsonAsync<TenantResponse>($"/api/tenants/{_tenantId}");
        tenant!.Name.Should().Be("Renamed Academy");
    }

    [Test]
    public async Task UpdateTenant_UnknownTenant_ReturnsNotFound()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PutAsJsonAsync($"/api/tenants/{Guid.NewGuid()}", new { name = "Doesn't Matter" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateTenant_WhitespaceName_ReturnsBadRequest()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PutAsJsonAsync($"/api/tenants/{_tenantId}", new { name = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteTenant_ExistingTenant_ReturnsNoContentAndTenantIsGone()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.DeleteAsync($"/api/tenants/{_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await superAdmin.GetAsync($"/api/tenants/{_tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteTenant_UnknownTenant_ReturnsNotFound()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.DeleteAsync($"/api/tenants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record TenantResponse(Guid Id, string Name, DateTime CreatedAt);
    private sealed record CreateTenantResponse(Guid TenantId);
}
