using Application.Common;
using FluentValidation;

namespace Application.Me.Commands;

public sealed class UpdateMyLanguageCommandValidator : AbstractValidator<UpdateMyLanguageCommand>
{
    public UpdateMyLanguageCommandValidator()
    {
        RuleFor(x => x.Language)
            .Must(SupportedLanguages.IsSupported)
            .WithMessage($"Language must be one of: {string.Join(", ", SupportedLanguages.All)}.");
    }
}
