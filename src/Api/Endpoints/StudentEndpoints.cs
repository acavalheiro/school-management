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
            request.DateOfBirth);

        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetStudent", new { id = result.Value }, result.Value)
            : Results.Problem(result.Error.Description, statusCode: 400);
    }
}

public record CreateStudentRequest(
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth);
