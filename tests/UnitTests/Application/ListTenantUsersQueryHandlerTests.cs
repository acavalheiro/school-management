using Application.Common.Interfaces;
using Application.Users.Queries;
using AwesomeAssertions;
using Domain.Common;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class ListTenantUsersQueryHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private ListTenantUsersQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _handler = new ListTenantUsersQueryHandler(_identityService.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_DelegatesToIdentityServiceListUsersAsyncWithRequestedTenantId()
    {
        // This handler has no ITenantService dependency at all — the target tenant is
        // whatever the SuperAdmin caller put in the request, never a caller-derived value.
        // Paired with ListUsersQueryHandlerTests, which forwards the caller's own tenant instead.
        var requestedTenantId = Guid.NewGuid();
        IReadOnlyList<UserDto> users = [new UserDto(Guid.NewGuid(), "user@school.test", "Admin")];
        _identityService
            .Setup(x => x.ListUsersAsync(requestedTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        var query = new ListTenantUsersQuery(requestedTenantId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(users);
        _identityService.Verify(
            x => x.ListUsersAsync(requestedTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
