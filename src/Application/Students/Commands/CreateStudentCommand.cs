using Application.Common.Mediator;
using Domain.Common;

namespace Application.Students.Commands;

public record CreateStudentCommand(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth) : ICommand<Guid>;
