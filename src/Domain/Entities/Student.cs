using Domain.Common;
using Domain.Enums;
using Domain.Events;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class Student : Entity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public StudentStatus Status { get; private set; }
    public Address? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    private Student() { }

    public static Result<Student> Create(
        string firstName,
        string lastName,
        string email,
        DateOnly dateOfBirth,
        Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Error.Validation(nameof(FirstName), "First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            return Error.Validation(nameof(LastName), "Last name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation(nameof(Email), "Email is required.");

        var student = new Student
        {
            TenantId = tenantId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            DateOfBirth = dateOfBirth,
            Status = StudentStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        student.RaiseDomainEvent(new StudentCreatedEvent(student.Id));
        return Result<Student>.Success(student);
    }

    public Result Update(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Error.Validation(nameof(FirstName), "First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            return Error.Validation(nameof(LastName), "Last name is required.");

        if (string.IsNullOrWhiteSpace(email))
            return Error.Validation(nameof(Email), "Email is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void Deactivate()
    {
        Status = StudentStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public string FullName => $"{FirstName} {LastName}";
}
