using Domain.Enums;

namespace Application.Students.Queries;

public record StudentDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    StudentStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
