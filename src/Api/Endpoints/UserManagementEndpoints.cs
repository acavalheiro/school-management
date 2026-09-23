using Application.Common;
using Application.Common.Mediator;
using Application.Users.Commands;
using Application.Users.Queries;

namespace Api.Endpoints;

/// <summary>Manages users within the caller's own tenant under <c>/api/users</c>. Admin only.</summary>
public static class UserManagementEndpoints
{
    /// <summary>Maps the list/update-role/delete endpoints under <c>/api/users</c>.</summary>
    public static void MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(AppRoles.Admin);

        group.MapGet("/", ListUsers)
            .WithName("ListUsers")
            .WithSummary("List all users (Admin only)");

        group.MapPut("/{id:guid}/role", UpdateRole)
            .WithName("UpdateUserRole")
            .WithSummary("Change a user's role (Admin only)");

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithName("DeleteUser")
            .WithSummary("Delete a user account (Admin only)");
    }

    private static async Task<IResult> ListUsers(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListUsersQuery(), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 500);
    }

    private static async Task<IResult> UpdateRole(
        Guid id,
        UpdateRoleRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateUserRoleCommand(id, request.Role), ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error.Description, statusCode: 400);
    }

    private static async Task<IResult> DeleteUser(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error.Description, statusCode: 400);
    }
}

/// <summary>New role for the target user. Must be one of <c>AppRoles.Assignable</c> (never <c>SuperAdmin</c>).</summary>
public record UpdateRoleRequest(string Role);
