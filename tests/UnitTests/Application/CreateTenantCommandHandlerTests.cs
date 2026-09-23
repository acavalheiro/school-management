using Application.Common.Interfaces;
using Application.Tenants.Commands;
using AwesomeAssertions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class CreateTenantCommandHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private Mock<DbSet<Tenant>> _tenants = null!;
    private CreateTenantCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _tenants = MockDbSet.Create<Tenant>([]);
        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(_tenants.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateTenantCommandHandler(_db.Object);
    }

    [Test]
    public async Task Handle_ValidName_CreatesTenantAndReturnsId()
    {
        var command = new CreateTenantCommand("Greenfield Academy");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _tenants.Verify(t => t.Add(It.Is<Tenant>(x => x.Name == "Greenfield Academy")), Times.Once);
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_WhitespaceName_ReturnsValidationFailureWithoutSaving()
    {
        var command = new CreateTenantCommand("   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
