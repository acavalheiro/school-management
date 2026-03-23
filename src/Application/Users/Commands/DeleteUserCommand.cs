using Application.Common.Mediator;
using Domain.Common;

namespace Application.Users.Commands;

public record DeleteUserCommand(Guid UserId) : ICommand;
