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
