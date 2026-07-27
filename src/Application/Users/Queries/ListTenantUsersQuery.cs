using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Queries;

// SuperAdmin-only: list users of an explicit tenant. The tenant is a parameter, not
// derived from the caller's claims (a SuperAdmin has no tenant).
public record ListTenantUsersQuery(Guid TenantId) : IQuery<IReadOnlyList<UserDto>>;
