using Application.Common;
using FluentValidation;

namespace Application.Users.Commands;

public sealed class CreateTenantUserCommandValidator : AbstractValidator<CreateTenantUserCommand>
{
    // Same whitelist enforced again in IdentityService.CreateUserAsync — the boundary
    // that keeps SuperAdmin off this path must survive a DI or pipeline regression.
    private static readonly string[] ValidRoles = AppRoles.Assignable;

    public CreateTenantUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => ValidRoles.Contains(r))
            .WithMessage($"Role must be one of: {string.Join(", ", ValidRoles)}.");
    }
}
