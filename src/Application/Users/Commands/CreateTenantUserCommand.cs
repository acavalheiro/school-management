using Application.Common.Mediator;

namespace Application.Users.Commands;

// SuperAdmin-only: provision a user in an explicit tenant. TenantId is the target
// tenant (the caller has none), not the caller's — see the endpoint authorization.
public record CreateTenantUserCommand(Guid TenantId, string Email, string Role) : ICommand<CreatedUserResponse>;

// The temporary password is surfaced exactly once, in this response, for the
// SuperAdmin to relay to the new user.
public record CreatedUserResponse(Guid UserId, string Email, string Role, string TemporaryPassword);
