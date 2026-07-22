using Application;
using Application.Auth.Commands;
using Application.Common;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Users.Commands;
using AwesomeAssertions;
using Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace UnitTests.Application;

/// <summary>
/// Guards the validation pipeline itself. The validators existed for a long time
/// without being wired into DI, which silently disabled every validation rule —
/// including the one preventing a tenant Admin from assigning themselves SuperAdmin.
/// </summary>
[TestFixture]
public sealed class ValidationPipelineTests
{
    private Mock<IIdentityService> _identityService = null!;
    private ServiceProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _identityService
            .Setup(x => x.UpdateUserRoleAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(x => x.TenantId).Returns(Guid.NewGuid());

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(_identityService.Object);
        services.AddSingleton(tenantService.Object);
        services.AddSingleton(new Mock<IAppDbContext>().Object);

        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    private Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
        _provider.GetRequiredService<IMediator>().Send(request, CancellationToken.None);

    [Test]
    public async Task Send_UpdateUserRoleToSuperAdmin_IsRejectedBeforeReachingTheHandler()
    {
        var result = await Send(new UpdateUserRoleCommand(Guid.NewGuid(), AppRoles.SuperAdmin));

        result.IsFailure.Should().BeTrue();
        _identityService.Verify(
            x => x.UpdateUserRoleAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Send_UpdateUserRoleToUnknownRole_IsRejectedBeforeReachingTheHandler()
    {
        var result = await Send(new UpdateUserRoleCommand(Guid.NewGuid(), "NotARole"));

        result.IsFailure.Should().BeTrue();
        _identityService.Verify(
            x => x.UpdateUserRoleAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Send_UpdateUserRoleToAssignableRole_ReachesTheHandler()
    {
        var userId = Guid.NewGuid();

        var result = await Send(new UpdateUserRoleCommand(userId, AppRoles.User));

        result.IsSuccess.Should().BeTrue();
        _identityService.Verify(
            x => x.UpdateUserRoleAsync(userId, AppRoles.User, It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Send_RegisterWithMismatchedConfirmPassword_IsRejected()
    {
        var command = new RegisterCommand("head@school.test", "Passw0rd!", "Different1!", "Test School");

        var result = await Send(command);

        result.IsFailure.Should().BeTrue();
        _identityService.Verify(
            x => x.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Send_RegisterWithWeakPassword_IsRejected()
    {
        var command = new RegisterCommand("head@school.test", "weak", "weak", "Test School");

        var result = await Send(command);

        result.IsFailure.Should().BeTrue();
    }
}
