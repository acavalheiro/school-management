using Application.Common.Mediator;

namespace Application.Me.Commands;

/// <summary>Changes the caller's own UI language. Carries no user id: it is always taken from the caller's token.</summary>
public record UpdateMyLanguageCommand(string Language) : ICommand;
