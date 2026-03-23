using Application.Common.Mediator;
using Domain.Common;

namespace Application.Tenants.Commands;

public record CreateTenantCommand(string Name) : ICommand<Guid>;
