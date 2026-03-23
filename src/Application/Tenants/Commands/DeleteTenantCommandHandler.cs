using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Commands;

public sealed class DeleteTenantCommandHandler(IAppDbContext db, IIdentityService identityService)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(
        DeleteTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant is null)
            return Error.NotFound(nameof(Tenant), request.TenantId);

        var students = await db.Students
            .Where(s => s.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        db.Students.RemoveRange(students);
        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync(cancellationToken);

        await identityService.DeleteTenantUsersAsync(request.TenantId, cancellationToken);

        return Result.Success();
    }
}
