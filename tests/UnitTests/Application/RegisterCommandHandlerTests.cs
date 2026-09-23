using Application.Auth.Commands;
using Application.Common.Interfaces;
using AwesomeAssertions;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using UnitTests.Common;

namespace UnitTests.Application;

[TestFixture]
public sealed class RegisterCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IAppDbContext> _db = null!;
    private Mock<DbSet<Tenant>> _tenants = null!;
    private Mock<IDbContextTransaction> _transaction = null!;
    private RegisterCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _tenants = MockDbSet.Create<Tenant>([]);
        _transaction = new Mock<IDbContextTransaction>();

        _identityService = new Mock<IIdentityService>();
        _db = new Mock<IAppDbContext>();
        _db.Setup(x => x.Tenants).Returns(_tenants.Object);
        _db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _db.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_transaction.Object);

        _handler = new RegisterCommandHandler(_identityService.Object, _db.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesTenantRegistersUserAndCommits()
    {
        var userId = Guid.NewGuid();
        _identityService
            .Setup(x => x.RegisterAsync(
                "admin@school.test", "Password123!", It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var command = new RegisterCommand("admin@school.test", "Password123!", "Password123!", "Greenfield Academy");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
        _tenants.Verify(t => t.Add(It.Is<Tenant>(x => x.Name == "Greenfield Academy")), Times.Once);
        _db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _identityService.Verify(
            x => x.RegisterAsync("admin@school.test", "Password123!", It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_WhitespaceTenantName_ReturnsValidationFailureWithoutOpeningTransaction()
    {
        var command = new RegisterCommand("admin@school.test", "Password123!", "Password123!", "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _db.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _identityService.Verify(
            x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_IdentityRegistrationFails_RollsBackTransactionAndReturnsError()
    {
        _identityService
            .Setup(x => x.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Error("Auth.RegistrationFailed", "Registration could not be completed."));

        var command = new RegisterCommand("dup@school.test", "Password123!", "Password123!", "Greenfield Academy");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.RegistrationFailed");
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
