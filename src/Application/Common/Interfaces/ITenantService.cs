namespace Application.Common.Interfaces;

/// <summary>
/// Resolves the caller's tenant context from the current request. Implemented by
/// reading the <c>tid</c> claim off <c>IHttpContextAccessor</c> in production;
/// tests read the same claim via <c>ClaimsTestTenantService</c> rather than stubbing a fixed value.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// The caller's tenant, or <see cref="Guid.Empty"/> when the request has no tenant
    /// context (anonymous requests, SuperAdmin, or a missing/malformed `tid` claim).
    /// <see cref="Guid.Empty"/> matches no rows — it does not widen access.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// True only for SuperAdmin. This is the one and only way to see across tenants;
    /// it is deliberately separate from <see cref="TenantId"/> so that an unresolved
    /// tenant fails closed instead of disabling the filter.
    /// </summary>
    bool CanBypassTenantFilter { get; }
}
