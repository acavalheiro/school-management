using Application.Common.Interfaces;
using Application.Tenants.Commands;
using AwesomeAssertions;
using Domain.Entities;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class UpdateTenantCommandHandlerTests
{
    private Mock<IAppDbContext> _db = null!;
    private UpdateTenantCommandHandler _handler = null!;
    private Tenant _tenant = null!;

    [SetUp]
    public void SetUp()
    {
        _tenant = Tenant.Create("Original Name").Value!;

        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([_tenant]).Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateTenantCommandHandler(_db.Object);
    }

    [Test]
    public async Task Handle_ExistingTenant_RenamesAndSaves()
    {
        var command = new UpdateTenantCommand(_tenant.Id, "Renamed Academy");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _tenant.Name.Should().Be("Renamed Academy");
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_UnknownTenant_ReturnsNotFoundWithoutSaving()
    {
        var command = new UpdateTenantCommand(Guid.NewGuid(), "Renamed Academy");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhitespaceName_ReturnsValidationFailureWithoutSaving()
    {
        var command = new UpdateTenantCommand(_tenant.Id, "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _tenant.Name.Should().Be("Original Name");
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
