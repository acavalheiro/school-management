using Application.Me.Commands;
using AwesomeAssertions;

namespace UnitTests.Application;

[TestFixture]
public sealed class UpdateMyLanguageCommandValidatorTests
{
    private readonly UpdateMyLanguageCommandValidator _validator = new();

    [TestCase("en-GB")]
    [TestCase("pt-PT")]
    public void Validate_SupportedLanguage_IsValid(string language) =>
        _validator.Validate(new UpdateMyLanguageCommand(language)).IsValid.Should().BeTrue();

    [TestCase("pt-BR")]
    [TestCase("en")]
    [TestCase("pt")]
    [TestCase("en-US")]
    [TestCase("PT-pt")]
    [TestCase("en-gb")]
    [TestCase(" en-GB")]
    [TestCase("")]
    [TestCase(null)]
    public void Validate_UnsupportedLanguage_IsInvalid(string? language) =>
        _validator.Validate(new UpdateMyLanguageCommand(language!)).IsValid.Should().BeFalse();
}
