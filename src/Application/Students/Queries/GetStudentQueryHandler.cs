using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Students.Queries;

public sealed class GetStudentQueryHandler(IAppDbContext db)
    : IRequestHandler<GetStudentQuery, Result<StudentDto>>
{
    public async Task<Result<StudentDto>> Handle(
        GetStudentQuery request,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);

        if (student is null)
            return Error.NotFound(nameof(student), request.StudentId);

        return new StudentDto(
            student.Id,
            student.FirstName,
            student.LastName,
            student.Email,
            student.DateOfBirth,
            student.Status,
            student.CreatedAt,
            student.UpdatedAt);
    }
}
