using Application.Common.Interfaces;
using Application.Tenants.Commands;
using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class DeleteTenantCommandHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private Mock<IIdentityService> _identityService = null!;
    private Mock<DbSet<Student>> _students = null!;
    private DeleteTenantCommandHandler _handler = null!;
    private Tenant _tenant = null!;

    [SetUp]
    public void SetUp()
    {
        _tenant = Tenant.Create("Greenfield Academy").Value!;

        _identityService = new Mock<IIdentityService>();
        _identityService
            .Setup(x => x.DeleteTenantUsersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([_tenant]).Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteTenantCommandHandler(_db.Object, _identityService.Object);
    }

    [Test]
    public async Task Handle_ExistingTenantWithStudents_RemovesTenantAndStudentsAndDeletesIdentityUsers()
    {
        var student = Student.Create("Jane", "Doe", "jane@example.com", new DateOnly(2010, 1, 1), _tenant.Id).Value!;
        var otherTenantStudent = Student.Create(
            "Other", "Kid", "other@example.com", new DateOnly(2011, 1, 1), Guid.NewGuid()).Value!;
        _students = MockDbSet.Create<Student>([student, otherTenantStudent]);
        _db.Setup(x => x.Students).Returns(_students.Object);

        var command = new DeleteTenantCommand(_tenant.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _students.Verify(
            s => s.RemoveRange(It.Is<IEnumerable<Student>>(list => list.Single().Id == student.Id)),
            Times.Once);
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _identityService.Verify(
            x => x.DeleteTenantUsersAsync(_tenant.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ExistingTenantWithNoStudents_RemovesTenantOnly()
    {
        _students = MockDbSet.Create<Student>([]);
        _db.Setup(x => x.Students).Returns(_students.Object);

        var command = new DeleteTenantCommand(_tenant.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _students.Verify(
            s => s.RemoveRange(It.Is<IEnumerable<Student>>(list => !list.Any())),
            Times.Once);
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UnknownTenant_ReturnsNotFoundWithoutTouchingIdentityService()
    {
        _students = MockDbSet.Create<Student>([]);
        _db.Setup(x => x.Students).Returns(_students.Object);

        var command = new DeleteTenantCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _identityService.Verify(
            x => x.DeleteTenantUsersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
