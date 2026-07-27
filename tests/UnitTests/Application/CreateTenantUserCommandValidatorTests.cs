using Application.Common;
using Application.Users.Commands;
using AwesomeAssertions;

namespace UnitTests.Application;

[TestFixture]
public sealed class CreateTenantUserCommandValidatorTests
{
    private readonly CreateTenantUserCommandValidator _validator = new();

    [Test]
    public void Validate_AdminRole_IsValid()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "staff@school.test", AppRoles.Admin);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_UserRole_IsValid()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "staff@school.test", AppRoles.User);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_SuperAdminRole_IsRejected()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "staff@school.test", AppRoles.SuperAdmin);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_EmptyEmail_IsRejected()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "", AppRoles.Admin);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_MalformedEmail_IsRejected()
    {
        var command = new CreateTenantUserCommand(Guid.NewGuid(), "not-an-email", AppRoles.Admin);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Test]
    public void Validate_EmptyTenant_IsRejected()
    {
        var command = new CreateTenantUserCommand(Guid.Empty, "staff@school.test", AppRoles.Admin);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
