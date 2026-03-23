using Application.Common.Interfaces;
using Application.Common.Mediator;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Students.Queries;

public sealed class ListStudentsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListStudentsQuery, Result<IReadOnlyList<StudentDto>>>
{
    public async Task<Result<IReadOnlyList<StudentDto>>> Handle(
        ListStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var students = await db.Students
            .AsNoTracking()
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentDto(
                s.Id,
                s.FirstName,
                s.LastName,
                s.Email,
                s.DateOfBirth,
                s.Status,
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StudentDto>>.Success(students);
    }
}
