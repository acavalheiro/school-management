using Application.Common.Interfaces;
using Application.Students.Commands;
using AwesomeAssertions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class CreateStudentCommandHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private Mock<DbSet<Student>> _students = null!;
    private Mock<ITenantService> _tenantService = null!;
    private CreateStudentCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _students = MockDbSet.Create<Student>([]);
        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Students).Returns(_students.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _tenantService = new Mock<ITenantService>();
        _tenantService.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _handler = new CreateStudentCommandHandler(_db.Object, _tenantService.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_AddsStudentAndReturnsId()
    {
        var command = new CreateStudentCommand("Jane", "Doe", "jane@example.com", new DateOnly(2000, 1, 1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _students.Verify(s => s.Add(It.Is<Student>(x => x.Email == "jane@example.com")), Times.Once);
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_EmptyFirstName_ReturnsFailureWithoutSaving()
    {
        var command = new CreateStudentCommand("", "Doe", "jane@example.com", new DateOnly(2000, 1, 1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_EmptyEmail_ReturnsFailureWithoutSaving()
    {
        var command = new CreateStudentCommand("Jane", "Doe", "", new DateOnly(2000, 1, 1));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
