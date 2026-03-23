using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Queries;

public record ListUsersQuery : IQuery<IReadOnlyList<UserDto>>;
