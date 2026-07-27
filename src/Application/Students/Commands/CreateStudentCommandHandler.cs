using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Students.Commands;

public sealed class CreateStudentCommandHandler(IAppDbContext db, ITenantService tenantService)
    : IRequestHandler<CreateStudentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateStudentCommand request,
        CancellationToken cancellationToken)
    {
        // A SuperAdmin has no tenant of their own, so they must name the target tenant
        // in the request. An Admin's tenant always comes from their claims — the request
        // field is never consulted, so an Admin cannot plant a student in another tenant.
        var effectiveTenantId = tenantService.CanBypassTenantFilter
            ? request.TenantId ?? Guid.Empty
            : tenantService.TenantId;

        // Without a tenant the record would be written but visible to nobody —
        // orphaned personal data. Reject rather than create it.
        if (effectiveTenantId == Guid.Empty)
            return Error.Validation("TenantId", "No tenant specified.");

        // A SuperAdmin bypasses the query filter, so an unknown tenant id would silently
        // create an unreachable, orphaned student. Verify the tenant exists first.
        if (tenantService.CanBypassTenantFilter &&
            !await db.Tenants.AnyAsync(t => t.Id == effectiveTenantId, cancellationToken))
            return Error.NotFound(nameof(Tenant), effectiveTenantId);

        var result = Student.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DateOfBirth,
            effectiveTenantId);

        if (result.IsFailure)
            return result.Error;

        db.Students.Add(result.Value!);
        await db.SaveChangesAsync(cancellationToken);

        return result.Value!.Id;
    }
}
