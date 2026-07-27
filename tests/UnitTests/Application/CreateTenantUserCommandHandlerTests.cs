using Application.Common;
using Application.Common.Interfaces;
using Application.Users.Commands;
using Application.Users.Queries;
using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class CreateTenantUserCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IAppDbContext> _db = null!;
    private CreateTenantUserCommandHandler _handler = null!;
    private Tenant _tenant = null!;

    [SetUp]
    public void SetUp()
    {
        _tenant = Tenant.Create("Greenfield Academy").Value!;

        _identityService = new Mock<IIdentityService>();
        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(MockDbSet.Create<Tenant>([_tenant]).Object);
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());

        _handler = new CreateTenantUserCommandHandler(_identityService.Object, _db.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_ReturnsTemporaryPasswordAndCallsCreateUser()
    {
        var userId = Guid.NewGuid();
        _identityService
            .Setup(x => x.CreateUserAsync("new@school.test", _tenant.Id, AppRoles.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatedUserDto(userId, "Temp!Pass123"));

        var command = new CreateTenantUserCommand(_tenant.Id, "new@school.test", AppRoles.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(userId);
        result.Value.TemporaryPassword.Should().Be("Temp!Pass123");
        result.Value.Email.Should().Be("new@school.test");
        result.Value.Role.Should().Be(AppRoles.Admin);
        _identityService.Verify(
            x => x.CreateUserAsync("new@school.test", _tenant.Id, AppRoles.Admin, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_UnknownTenant_ReturnsNotFoundWithoutCreatingUser()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "new@school.test", AppRoles.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
        _identityService.Verify(
            x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_IdentityFailure_PropagatesError()
    {
        _identityService
            .Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Conflict("ApplicationUser"));

        var command = new CreateTenantUserCommand(_tenant.Id, "dup@school.test", AppRoles.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
