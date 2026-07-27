using System.Net;
using System.Net.Http.Json;
using Application.Common;
using AwesomeAssertions;
using Domain.Entities;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>
/// Who may create students, and how the target tenant is resolved: an Admin from
/// their claims (a body tenant is ignored), a SuperAdmin from an explicit tenant.
/// </summary>
[TestFixture]
public sealed class StudentCreationAuthorizationTests
{
    private static readonly Guid TenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private WebAppFactory _factory = null!;
    private Guid _seededTenantId;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();
        await _factory.SeedAsync(db =>
        {
            var tenant = Tenant.Create("Seeded School").Value!;
            _seededTenantId = tenant.Id;
            db.Tenants.Add(tenant);
            return Task.CompletedTask;
        });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task CreateStudent_AsUserRole_IsForbidden()
    {
        using var client = _factory.CreateClientFor(TenantA, AppRoles.User);

        var response = await client.PostAsJsonAsync("/api/students", new
        {
            firstName = "Read",
            lastName = "Only",
            email = "readonly@school-a.test",
            dateOfBirth = "2015-01-01"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateStudent_AsAdmin_IsCreated()
    {
        using var client = _factory.CreateClientFor(TenantA, AppRoles.Admin);

        var response = await client.PostAsJsonAsync("/api/students", new
        {
            firstName = "Ana",
            lastName = "Silva",
            email = "ana@school-a.test",
            dateOfBirth = "2015-04-02"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task CreateStudent_AsSuperAdmin_WithTenantId_CreatesUnderThatTenant()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/students", new
        {
            firstName = "Bruno",
            lastName = "Costa",
            email = "bruno@seeded.test",
            dateOfBirth = "2014-09-08",
            tenantId = _seededTenantId
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var seededTenantClient = _factory.CreateClientFor(_seededTenantId, AppRoles.Admin);
        var students = await seededTenantClient.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        students.Should().ContainSingle(s => s.Email == "bruno@seeded.test");
    }

    [Test]
    public async Task CreateStudent_AsSuperAdmin_WithoutTenantId_IsBadRequest()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/students", new
        {
            firstName = "No",
            lastName = "Tenant",
            email = "notenant@nowhere.test",
            dateOfBirth = "2015-01-01"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateStudent_AsSuperAdmin_WithUnknownTenant_IsNotFound()
    {
        using var superAdmin = _factory.CreateClientWithoutTenant(AppRoles.SuperAdmin);

        var response = await superAdmin.PostAsJsonAsync("/api/students", new
        {
            firstName = "Ghost",
            lastName = "Tenant",
            email = "ghost@nowhere.test",
            dateOfBirth = "2015-01-01",
            tenantId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateStudent_AsAdmin_IgnoresTenantIdInBody()
    {
        // An Admin of tenant A names tenant B in the body; the student must still land
        // in tenant A, not B.
        using var adminOfA = _factory.CreateClientFor(TenantA, AppRoles.Admin);

        var response = await adminOfA.PostAsJsonAsync("/api/students", new
        {
            firstName = "Carla",
            lastName = "Dias",
            email = "carla@school-a.test",
            dateOfBirth = "2016-03-11",
            tenantId = TenantB
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var clientA = _factory.CreateClientFor(TenantA, AppRoles.Admin);
        var seenByA = await clientA.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        seenByA.Should().ContainSingle(s => s.Email == "carla@school-a.test");

        using var clientB = _factory.CreateClientFor(TenantB, AppRoles.Admin);
        var seenByB = await clientB.GetFromJsonAsync<List<StudentResponse>>("/api/students");
        seenByB.Should().NotContain(s => s.Email == "carla@school-a.test");
    }

    private sealed record StudentResponse(Guid Id, string Email);
}
