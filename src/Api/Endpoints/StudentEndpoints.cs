using Application.Common;
using Application.Common.Mediator;
using Application.Students.Commands;
using Application.Students.Queries;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class StudentEndpoints
{
    public static void MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students").RequireAuthorization();

        group.MapGet("/", ListStudents)
            .WithName("ListStudents")
            .WithSummary("Get all students");

        group.MapGet("/{id:guid}", GetStudent)
            .WithName("GetStudent")
            .WithSummary("Get a student by ID");

        group.MapPost("/", CreateStudent)
            .RequireAuthorization(AppPolicies.StudentWrite)
            .WithName("CreateStudent")
            .WithSummary("Create a new student");
    }

    private static async Task<IResult> ListStudents(
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ListStudentsQuery(), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 400);
    }

    private static async Task<IResult> GetStudent(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetStudentQuery(id), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Description);
    }

    private static async Task<IResult> CreateStudent(
        CreateStudentRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateStudentCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DateOfBirth,
            request.TenantId);

        var result = await mediator.Send(command, ct);

        if (result.IsSuccess)
            return Results.CreatedAtRoute("GetStudent", new { id = result.Value }, result.Value);

        // A SuperAdmin naming a tenant that does not exist is a not-found, distinct from
        // a validation failure (e.g. a missing tenant context or invalid field).
        return result.Error.Code.Contains("NotFound")
            ? Results.NotFound(result.Error.Description)
            : Results.Problem(result.Error.Description, statusCode: 400);
    }
}

// TenantId is used only for a SuperAdmin caller, who must pick the target tenant;
// it is ignored for an Admin, whose tenant comes from their claims.
public record CreateStudentRequest(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    Guid? TenantId = null);
