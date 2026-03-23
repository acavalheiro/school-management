using Application.Common.Mediator;
using Domain.Common;

namespace Application.Tenants.Queries;

public record GetTenantQuery(Guid TenantId) : IQuery<TenantDto>;
