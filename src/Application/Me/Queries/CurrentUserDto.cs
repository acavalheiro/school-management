namespace Application.Me.Queries;

public record CurrentUserDto(Guid Id, string Email, string Role, string PreferredLanguage);
