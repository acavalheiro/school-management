using Application.Common.Interfaces;
using Application.Users.Queries;
using AwesomeAssertions;
using Domain.Common;
using Moq;

namespace UnitTests.Application;

[TestFixture]
public sealed class ListUsersQueryHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<ITenantService> _tenantService = null!;
    private ListUsersQueryHandler _handler = null!;
    private readonly Guid _callerTenantId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _tenantService = new Mock<ITenantService>();
        _tenantService.Setup(x => x.TenantId).Returns(_callerTenantId);

        _handler = new ListUsersQueryHandler(_identityService.Object, _tenantService.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_DelegatesToIdentityServiceListUsersAsyncWithCallerTenantId()
    {
        // ListUsersQuery carries no tenant id of its own — unlike ListTenantUsersQuery, this
        // handler always forwards the caller's own tenant from ITenantService.
        IReadOnlyList<UserDto> users = [new UserDto(Guid.NewGuid(), "user@school.test", "User")];
        _identityService
            .Setup(x => x.ListUsersAsync(_callerTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<UserDto>>.Success(users));

        var result = await _handler.Handle(new ListUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(users);
        _identityService.Verify(
            x => x.ListUsersAsync(_callerTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
