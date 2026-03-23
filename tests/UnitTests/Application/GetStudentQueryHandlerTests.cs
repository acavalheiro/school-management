using Application.Common.Interfaces;
using Application.Students.Queries;
using AwesomeAssertions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class GetStudentQueryHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private GetStudentQueryHandler _handler = null!;

    private static Student MakeStudent(string first, string last, string email) =>
        Student.Create(first, last, email, new DateOnly(2000, 1, 1), Guid.NewGuid()).Value!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IAppDbContext>();
        _handler = new GetStudentQueryHandler(_db.Object);
    }

    [Test]
    public async Task Handle_ExistingId_ReturnsStudentDto()
    {
        var student = MakeStudent("Alice", "Smith", "alice@example.com");
        var mockSet = MockDbSet.Create<Student>([student]);
        _db.Setup(x => x.Students).Returns(mockSet.Object);

        var result = await _handler.Handle(new GetStudentQuery(student.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("alice@example.com");
        result.Value.FirstName.Should().Be("Alice");
    }

    [Test]
    public async Task Handle_NonExistentId_ReturnsFailure()
    {
        var mockSet = MockDbSet.Create<Student>([]);
        _db.Setup(x => x.Students).Returns(mockSet.Object);

        var result = await _handler.Handle(new GetStudentQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
