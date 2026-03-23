using Application.Common.Mediator;
using Domain.Common;

namespace Application.Tenants.Commands;

public record UpdateTenantCommand(Guid TenantId, string Name) : ICommand;
