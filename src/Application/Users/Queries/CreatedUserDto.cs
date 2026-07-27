namespace Application.Users.Queries;

// Returned once when a SuperAdmin provisions a user. The temporary password is
// generated server-side and never persisted in plaintext, so this is the only
// moment it is available — the caller must relay it to the new user.
public record CreatedUserDto(Guid UserId, string TemporaryPassword);
