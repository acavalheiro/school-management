using Application.Auth.Queries;
using Application.Common.Mediator;
using Domain.Common;

namespace Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : ICommand<AuthTokenDto>;
