using Application.Common;
using Application.Common.Mediator;
using Application.Tenants.Commands;
using Application.Tenants.Queries;
using Application.Users.Commands;
using Application.Users.Queries;

namespace Api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants")
            .RequireAuthorization(AppRoles.SuperAdmin);

        group.MapGet("/", ListTenants)
            .WithName("ListTenants")
            .WithSummary("List all tenants");

        group.MapGet("/{id:guid}", GetTenant)
            .WithName("GetTenant")
            .WithSummary("Get a tenant by ID");

        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Create a new tenant");

        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Rename a tenant");

        group.MapDelete("/{id:guid}", DeleteTenant)
            .WithName("DeleteTenant")
            .WithSummary("Delete a tenant and all its data");

        group.MapGet("/{id:guid}/users", ListTenantUsers)
            .WithName("ListTenantUsers")
            .WithSummary("List the users of a tenant");

        group.MapPost("/{id:guid}/users", CreateTenantUser)
            .WithName("CreateTenantUser")
            .WithSummary("Create an Admin or User in a tenant with a temporary password");
    }

    private static async Task<IResult> ListTenants(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListTenantsQuery(), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 500);
    }

    private static async Task<IResult> GetTenant(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTenantQuery(id), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.Error.Description);
    }

    private static async Task<IResult> CreateTenant(
        CreateTenantRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTenantCommand(request.Name), ct);
        return result.IsSuccess
            ? Results.Created($"/api/tenants/{result.Value}", new { tenantId = result.Value })
            : Results.Problem(result.Error.Description, statusCode: 400);
    }

    private static async Task<IResult> UpdateTenant(
        Guid id,
        UpdateTenantRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateTenantCommand(id, request.Name), ct);
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.Code.Contains("NotFound")
                ? Results.NotFound(result.Error.Description)
                : Results.Problem(result.Error.Description, statusCode: 400);
    }

    private static async Task<IResult> DeleteTenant(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteTenantCommand(id), ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.Error.Description);
    }

    private static async Task<IResult> ListTenantUsers(Guid id, IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new ListTenantUsersQuery(id), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 500);
    }

    private static async Task<IResult> CreateTenantUser(
        Guid id,
        CreateTenantUserRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTenantUserCommand(id, request.Email, request.Role), ct);

        if (result.IsSuccess)
            return Results.Created($"/api/tenants/{id}/users/{result.Value!.UserId}", result.Value);

        return result.Error.Code.Contains("NotFound")
            ? Results.NotFound(result.Error.Description)
            : Results.Problem(result.Error.Description, statusCode: 400);
    }
}

public record CreateTenantRequest(string Name);
public record UpdateTenantRequest(string Name);
public record CreateTenantUserRequest(string Email, string Role);
