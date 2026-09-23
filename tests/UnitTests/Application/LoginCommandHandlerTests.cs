using Application.Auth.Commands;
using Application.Auth.Queries;
using Application.Common.Interfaces;
using AwesomeAssertions;
using Domain.Common;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class LoginCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private LoginCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _handler = new LoginCommandHandler(_identityService.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_DelegatesToIdentityServiceLoginAsyncAndReturnsToken()
    {
        var token = new AuthTokenDto("jwt-token", DateTime.UtcNow.AddMinutes(30));
        _identityService
            .Setup(x => x.LoginAsync("user@school.test", "Password123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);

        var command = new LoginCommand("user@school.test", "Password123!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(token);
        _identityService.Verify(
            x => x.LoginAsync("user@school.test", "Password123!", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_IdentityServiceFailure_PropagatesError()
    {
        _identityService
            .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Error("Auth.InvalidCredentials", "Invalid email or password."));

        var command = new LoginCommand("user@school.test", "wrong-password");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }
}
