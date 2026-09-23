using Application.Common;
using Application.Common.Interfaces;
using Application.Users.Commands;
using AwesomeAssertions;
using Domain.Common;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class UpdateUserRoleCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<ITenantService> _tenantService = null!;
    private UpdateUserRoleCommandHandler _handler = null!;
    private readonly Guid _callerTenantId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _tenantService = new Mock<ITenantService>();
        _tenantService.Setup(x => x.TenantId).Returns(_callerTenantId);

        _handler = new UpdateUserRoleCommandHandler(_identityService.Object, _tenantService.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_DelegatesToIdentityServiceWithCallerTenantId()
    {
        var userId = Guid.NewGuid();
        _identityService
            .Setup(x => x.UpdateUserRoleAsync(userId, AppRoles.Admin, _callerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var command = new UpdateUserRoleCommand(userId, AppRoles.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _identityService.Verify(
            x => x.UpdateUserRoleAsync(userId, AppRoles.Admin, _callerTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_IdentityServiceFailure_PropagatesError()
    {
        var userId = Guid.NewGuid();
        _identityService
            .Setup(x => x.UpdateUserRoleAsync(userId, AppRoles.Admin, _callerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("ApplicationUser", userId));

        var command = new UpdateUserRoleCommand(userId, AppRoles.Admin);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
