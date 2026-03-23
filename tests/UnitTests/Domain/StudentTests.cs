using AwesomeAssertions;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;

namespace UnitTests.Domain;

[TestFixture]
public sealed class StudentTests
{
    [Test]
    public void Create_ValidInput_ReturnsSuccessWithActiveStatus()
    {
        // Arrange
        var dob = new DateOnly(2000, 1, 1);

        // Act
        var result = Student.Create("John", "Doe", "john@example.com", dob, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Email.Should().Be("john@example.com");
        result.Value.Status.Should().Be(StudentStatus.Active);
    }

    [Test]
    public void Create_EmptyFirstName_ReturnsFailure()
    {
        var result = Student.Create(string.Empty, "Doe", "john@example.com", new DateOnly(2000, 1, 1), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Test]
    public void Create_ValidStudent_RaisesStudentCreatedEvent()
    {
        var result = Student.Create("Jane", "Doe", "jane@example.com", new DateOnly(1999, 5, 15), Guid.NewGuid());

        result.Value!.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentCreatedEvent>();
    }

    [Test]
    public void Create_EmailIsLowercased()
    {
        var result = Student.Create("Jane", "Doe", "JANE@EXAMPLE.COM", new DateOnly(1999, 5, 15), Guid.NewGuid());

        result.Value!.Email.Should().Be("jane@example.com");
    }

    [Test]
    public void Deactivate_ActiveStudent_SetsStatusToInactive()
    {
        var student = Student.Create("John", "Doe", "john@example.com", new DateOnly(2000, 1, 1), Guid.NewGuid()).Value!;

        student.Deactivate();

        student.Status.Should().Be(StudentStatus.Inactive);
    }
}
