using Application.Common.Mediator;
using Domain.Common;

namespace Application.Auth.Commands;

public record RegisterCommand(string Email, string Password, string ConfirmPassword, string TenantName) : ICommand<Guid>;
