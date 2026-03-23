using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using IntegrationTests.Common;

namespace IntegrationTests.Endpoints;

[TestFixture]
public sealed class StudentEndpointsTests
{
    private WebAppFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebAppFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task ListStudents_EmptyDatabase_ReturnsOkWithEmptyList()
    {
        var response = await _client.GetAsync("/api/students");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var students = await response.Content.ReadFromJsonAsync<List<object>>();
        students.Should().BeEmpty();
    }

    [Test]
    public async Task CreateStudent_ValidRequest_ReturnsCreated()
    {
        var request = new
        {
            firstName = "John",
            lastName = "Doe",
            email = "john.doe@example.com",
            dateOfBirth = "2000-01-15"
        };

        var response = await _client.PostAsJsonAsync("/api/students", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Test]
    public async Task GetStudent_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/students/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
