using Application.Common.Interfaces;
using Application.Students.Queries;
using AwesomeAssertions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class ListStudentsQueryHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private ListStudentsQueryHandler _handler = null!;

    private static Student MakeStudent(string first, string last, string email) =>
        Student.Create(first, last, email, new DateOnly(2000, 1, 1), Guid.NewGuid()).Value!;

    [SetUp]
    public void SetUp()
    {
        _db = new Mock<IAppDbContext>();
        _handler = new ListStudentsQueryHandler(_db.Object);
    }

    [Test]
    public async Task Handle_EmptyDatabase_ReturnsEmptyList()
    {
        _db.Setup(x => x.Students).Returns(MockDbSet.Create<Student>([]).Object);

        var result = await _handler.Handle(new ListStudentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_MultipleStudents_ReturnsAll()
    {
        var data = new List<Student>
        {
            MakeStudent("Charlie", "Brown", "charlie@example.com"),
            MakeStudent("Alice", "Smith", "alice@example.com"),
        };
        _db.Setup(x => x.Students).Returns(MockDbSet.Create<Student>(data).Object);

        var result = await _handler.Handle(new ListStudentsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_Students_ReturnsSortedByLastNameThenFirstName()
    {
        var data = new List<Student>
        {
            MakeStudent("Charlie", "Smith", "charlie@example.com"),
            MakeStudent("Alice", "Brown", "alice@example.com"),
            MakeStudent("Bob", "Brown", "bob@example.com"),
        };
        _db.Setup(x => x.Students).Returns(MockDbSet.Create<Student>(data).Object);

        var result = await _handler.Handle(new ListStudentsQuery(), CancellationToken.None);

        var names = result.Value!.Select(s => $"{s.LastName},{s.FirstName}").ToList();
        names.Should().Equal("Brown,Alice", "Brown,Bob", "Smith,Charlie");
    }
}
