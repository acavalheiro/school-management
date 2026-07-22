using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Domain.Entities;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

/// <summary>
/// The core privacy invariant: one school must never observe another school's
/// student records. These tests drive the real tenant-resolution path (the `tid`
/// claim feeding the EF Core global query filter) rather than a fixed stub.
/// </summary>
[TestFixture]
public sealed class TenantIsolationTests
{
    private static readonly Guid SchoolA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SchoolB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private WebAppFactory _factory = null!;
    private Guid _schoolAStudentId;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new WebAppFactory();

        await _factory.SeedAsync(db =>
        {
            var studentA = Student.Create("Ana", "Silva", "ana@school-a.test", new DateOnly(2015, 4, 2), SchoolA).Value!;
            var studentB = Student.Create("Bruno", "Costa", "bruno@school-b.test", new DateOnly(2014, 9, 8), SchoolB).Value!;

            _schoolAStudentId = studentA.Id;
            db.Students.AddRange(studentA, studentB);
            return Task.CompletedTask;
        });
    }

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task ListStudents_AsSchoolB_DoesNotReturnSchoolAStudents()
    {
        using var client = _factory.CreateClientFor(SchoolB);

        var students = await client.GetFromJsonAsync<List<StudentResponse>>("/api/students");

        students.Should().ContainSingle();
        students![0].Email.Should().Be("bruno@school-b.test");
    }

    [Test]
    public async Task ListStudents_AsSchoolA_DoesNotReturnSchoolBStudents()
    {
        using var client = _factory.CreateClientFor(SchoolA);

        var students = await client.GetFromJsonAsync<List<StudentResponse>>("/api/students");

        students.Should().ContainSingle();
        students![0].Email.Should().Be("ana@school-a.test");
    }

    [Test]
    public async Task GetStudent_AsSchoolB_CannotReadSchoolAStudentById()
    {
        using var client = _factory.CreateClientFor(SchoolB);

        var response = await client.GetAsync($"/api/students/{_schoolAStudentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateStudent_StampsTheCallersTenantAndStaysInvisibleToOthers()
    {
        using var schoolBClient = _factory.CreateClientFor(SchoolB);
        var created = await schoolBClient.PostAsJsonAsync("/api/students", new
        {
            firstName = "Carla",
            lastName = "Dias",
            email = "carla@school-b.test",
            dateOfBirth = "2016-03-11"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        using var schoolAClient = _factory.CreateClientFor(SchoolA);
        var studentsSeenByA = await schoolAClient.GetFromJsonAsync<List<StudentResponse>>("/api/students");

        studentsSeenByA.Should().NotContain(s => s.Email == "carla@school-b.test");
    }

    private sealed record StudentResponse(Guid Id, string Email);
}
