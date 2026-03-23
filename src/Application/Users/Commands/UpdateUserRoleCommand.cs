using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Commands;

public record UpdateUserRoleCommand(Guid UserId, string Role) : ICommand;
