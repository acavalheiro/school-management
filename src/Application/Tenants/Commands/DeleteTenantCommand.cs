using Application.Common.Mediator;
using Domain.Common;

namespace Application.Tenants.Commands;

public record DeleteTenantCommand(Guid TenantId) : ICommand;
