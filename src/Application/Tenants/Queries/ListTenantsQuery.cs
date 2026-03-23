using Application.Common.Mediator;
using Domain.Common;

namespace Application.Tenants.Queries;

public record ListTenantsQuery : IQuery<IReadOnlyList<TenantDto>>;
