using System.Net;
using System.Net.Http.Json;
using Application.Common;
using AwesomeAssertions;
using Domain.Entities;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>
/// SuperAdmin provisioning of tenant users, and the cross-tenant guard on
/// <c>/api/users</c> that — until this feature — had no second user in a tenant to
/// exercise it against.
/// </summary>
[TestFixture]
public sealed class TenantUserManagementTests
{
    private WebAppFactory _factory = null!;
    private Guid _tenantBId;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();
        await _factory.SeedRolesAsync(AppRoles.Admin, AppRoles.User, AppRoles.SuperAdmin);

        await _factory.SeedAsync(db =>
        {
            var tenantB = Tenant.Create("School B").Value!;
            _tenantBId = tenantB.Id;
            db.Tenants.Add(tenantB);
            return Task.CompletedTask;
        });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task CreateTenantUser_AsSuperAdmin_ReturnsTempPasswordAndListsTheUser()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync(
            $"/api/tenants/{_tenantBId}/users",
            new { email = "admin@school-b.test", role = AppRoles.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreatedUserResponse>();
        created!.TemporaryPassword.Should().NotBeNullOrWhiteSpace();
        created.Email.Should().Be("admin@school-b.test");

        var users = await superAdmin.GetFromJsonAsync<List<UserResponse>>($"/api/tenants/{_tenantBId}/users");
        users.Should().ContainSingle(u => u.Email == "admin@school-b.test" && u.Role == AppRoles.Admin);
    }

    [Test]
    public async Task CreateTenantUser_WithSuperAdminRole_IsRejected()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync(
            $"/api/tenants/{_tenantBId}/users",
            new { email = "escalate@school-b.test", role = AppRoles.SuperAdmin });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateTenantUser_ForUnknownTenant_ReturnsNotFound()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync(
            $"/api/tenants/{Guid.NewGuid()}/users",
            new { email = "nobody@nowhere.test", role = AppRoles.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateTenantUser_AsAdmin_IsForbidden()
    {
        // The endpoint group is SuperAdmin-only; an Admin token must not reach it.
        using var admin = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);

        var response = await admin.PostAsJsonAsync(
            $"/api/tenants/{_tenantBId}/users",
            new { email = "admin@school-b.test", role = AppRoles.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminOfAnotherTenant_CannotChangeRoleOrDeleteUserInTenantB()
    {
        // Provision a real second user inside tenant B.
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);
        var created = await (await superAdmin.PostAsJsonAsync(
                $"/api/tenants/{_tenantBId}/users",
                new { email = "staff@school-b.test", role = AppRoles.User }))
            .Content.ReadFromJsonAsync<CreatedUserResponse>();

        var tenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var adminOfA = _factory.CreateClientFor(tenantA, AppRoles.Admin);

        // The guard reports the cross-tenant target as not found; the endpoint surfaces
        // that as a non-success status. Either way the operation must be blocked.
        var roleChange = await adminOfA.PutAsJsonAsync(
            $"/api/users/{created!.UserId}/role", new { role = AppRoles.Admin });
        roleChange.IsSuccessStatusCode.Should().BeFalse();

        var delete = await adminOfA.DeleteAsync($"/api/users/{created.UserId}");
        delete.IsSuccessStatusCode.Should().BeFalse();

        // The user in tenant B is untouched: still present, still a plain User.
        var users = await superAdmin.GetFromJsonAsync<List<UserResponse>>($"/api/tenants/{_tenantBId}/users");
        users.Should().ContainSingle(u => u.Id == created.UserId && u.Role == AppRoles.User);
    }

    private sealed record CreatedUserResponse(Guid UserId, string Email, string Role, string TemporaryPassword);
    private sealed record UserResponse(Guid Id, string Email, string Role);
}
