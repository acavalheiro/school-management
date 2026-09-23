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

    [Test]
    public async Task ListUsers_AsAdmin_ReturnsOnlyUsersInCallersTenant()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var tenantBUser = await (await superAdmin.PostAsJsonAsync(
                $"/api/tenants/{_tenantBId}/users",
                new { email = "inb@school-b.test", role = AppRoles.User }))
            .Content.ReadFromJsonAsync<CreatedUserResponse>();

        Guid tenantCId = Guid.Empty;
        await _factory.SeedAsync(db =>
        {
            var tenantC = Tenant.Create("School C").Value!;
            tenantCId = tenantC.Id;
            db.Tenants.Add(tenantC);
            return Task.CompletedTask;
        });
        await superAdmin.PostAsJsonAsync(
            $"/api/tenants/{tenantCId}/users",
            new { email = "inc@school-c.test", role = AppRoles.User });

        using var adminOfB = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);
        var users = await adminOfB.GetFromJsonAsync<List<UserResponse>>("/api/users");

        users.Should().ContainSingle(u => u.Id == tenantBUser!.UserId);
        users.Should().NotContain(u => u.Email == "inc@school-c.test");
    }

    [Test]
    public async Task ListUsers_AsUser_IsForbidden()
    {
        using var user = _factory.CreateClientFor(_tenantBId, AppRoles.User);

        var response = await user.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task UpdateUserRole_AsAdminOfSameTenant_ReturnsNoContentAndChangesTheRole()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);
        var created = await (await superAdmin.PostAsJsonAsync(
                $"/api/tenants/{_tenantBId}/users",
                new { email = "promote@school-b.test", role = AppRoles.User }))
            .Content.ReadFromJsonAsync<CreatedUserResponse>();

        using var adminOfB = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);
        var response = await adminOfB.PutAsJsonAsync(
            $"/api/users/{created!.UserId}/role", new { role = AppRoles.Admin });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var users = await superAdmin.GetFromJsonAsync<List<UserResponse>>($"/api/tenants/{_tenantBId}/users");
        users.Should().ContainSingle(u => u.Id == created.UserId && u.Role == AppRoles.Admin);
    }

    [Test]
    public async Task UpdateUserRole_ToSuperAdmin_IsRejected()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);
        var created = await (await superAdmin.PostAsJsonAsync(
                $"/api/tenants/{_tenantBId}/users",
                new { email = "escalate-attempt@school-b.test", role = AppRoles.User }))
            .Content.ReadFromJsonAsync<CreatedUserResponse>();

        using var adminOfB = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);

        // Through the real HTTP pipeline this is always caught by UpdateUserRoleCommandValidator
        // before the request reaches IdentityService.UpdateUserRoleAsync's own whitelist check —
        // the validator runs first in the pipeline, so this test cannot distinguish which of the
        // two layers rejected it. It only proves the end-to-end behavior: escalation is blocked.
        var response = await adminOfB.PutAsJsonAsync(
            $"/api/users/{created!.UserId}/role", new { role = AppRoles.SuperAdmin });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task DeleteUser_AsAdminOfSameTenant_ReturnsNoContentAndUserIsGone()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);
        var created = await (await superAdmin.PostAsJsonAsync(
                $"/api/tenants/{_tenantBId}/users",
                new { email = "remove@school-b.test", role = AppRoles.User }))
            .Content.ReadFromJsonAsync<CreatedUserResponse>();

        using var adminOfB = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);
        var response = await adminOfB.DeleteAsync($"/api/users/{created!.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var users = await superAdmin.GetFromJsonAsync<List<UserResponse>>($"/api/tenants/{_tenantBId}/users");
        users.Should().NotContain(u => u.Id == created.UserId);
    }

    [Test]
    public async Task DeleteUser_UnknownUserId_ReturnsBadRequest()
    {
        using var adminOfB = _factory.CreateClientFor(_tenantBId, AppRoles.Admin);

        // The handler surfaces "not found" as a generic Error, and the endpoint maps any
        // failure to 400 (see UserManagementEndpoints.DeleteUser) — not 404.
        var response = await adminOfB.DeleteAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record CreatedUserResponse(Guid UserId, string Email, string Role, string TemporaryPassword);
    private sealed record UserResponse(Guid Id, string Email, string Role);
}
