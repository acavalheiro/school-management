using Application.Common.Mediator;
using Domain.Common;

namespace Application.Students.Commands;

// TenantId is honoured only for a SuperAdmin caller (who has no tenant of their own);
// for an Admin it is ignored and the tenant comes from their claims. See the handler.
public record CreateStudentCommand(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    Guid? TenantId = null) : ICommand<Guid>;
