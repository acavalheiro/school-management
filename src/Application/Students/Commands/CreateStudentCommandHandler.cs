using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Domain.Entities;

namespace Application.Students.Commands;

public sealed class CreateStudentCommandHandler(IAppDbContext db, ITenantService tenantService)
    : IRequestHandler<CreateStudentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateStudentCommand request,
        CancellationToken cancellationToken)
    {
        // Without a tenant the record would be written but visible to nobody —
        // orphaned personal data. Reject rather than create it.
        if (tenantService.TenantId == Guid.Empty)
            return Error.Validation("TenantId", "The request has no tenant context.");

        var result = Student.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DateOfBirth,
            tenantService.TenantId);

        if (result.IsFailure)
            return result.Error;

        db.Students.Add(result.Value!);
        await db.SaveChangesAsync(cancellationToken);

        return result.Value!.Id;
    }
}
