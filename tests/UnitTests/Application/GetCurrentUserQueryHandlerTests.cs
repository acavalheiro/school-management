using Application.Common;
using Application.Common.Interfaces;
using Application.Me.Queries;
using AwesomeAssertions;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class GetCurrentUserQueryHandlerTests
{
    [Test]
    public async Task Handle_Always_LoadsTheCallerFromTheirToken()
    {
        var callerId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(callerId);

        var dto = new CurrentUserDto(callerId, "me@school.test", AppRoles.User, SupportedLanguages.PtPt);
        var identityService = new Mock<IIdentityService>();
        identityService
            .Setup(x => x.GetCurrentUserAsync(callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var handler = new GetCurrentUserQueryHandler(identityService.Object, currentUser.Object);

        var result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
    }
}
