using Application.Common;
using Application.Common.Interfaces;
using Application.Me.Commands;
using AwesomeAssertions;
using Domain.Common;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class UpdateMyLanguageCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private UpdateMyLanguageCommandHandler _handler = null!;
    private readonly Guid _callerId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(_callerId);

        _handler = new UpdateMyLanguageCommandHandler(_identityService.Object, currentUser.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_UpdatesTheCallersOwnAccount()
    {
        _identityService
            .Setup(x => x.UpdatePreferredLanguageAsync(_callerId, SupportedLanguages.PtPt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _handler.Handle(new UpdateMyLanguageCommand(SupportedLanguages.PtPt), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _identityService.Verify(
            x => x.UpdatePreferredLanguageAsync(_callerId, SupportedLanguages.PtPt, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_IdentityServiceFailure_PropagatesError()
    {
        _identityService
            .Setup(x => x.UpdatePreferredLanguageAsync(_callerId, SupportedLanguages.PtPt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("ApplicationUser", _callerId));

        var result = await _handler.Handle(new UpdateMyLanguageCommand(SupportedLanguages.PtPt), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }
}
