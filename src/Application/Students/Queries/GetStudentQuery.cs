using Application.Common.Mediator;
using Domain.Common;

namespace Application.Students.Queries;

public record GetStudentQuery(Guid StudentId) : IQuery<StudentDto>;
