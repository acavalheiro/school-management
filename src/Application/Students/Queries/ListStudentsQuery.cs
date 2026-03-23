using Application.Common.Mediator;
using Domain.Common;

namespace Application.Students.Queries;

public record ListStudentsQuery : IQuery<IReadOnlyList<StudentDto>>;
