namespace Application.Auth.Queries;

public record AuthTokenDto(string Token, DateTime ExpiresAt);
