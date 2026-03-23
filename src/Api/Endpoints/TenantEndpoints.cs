using Application.Common;
using Application.Common.Mediator;
using Application.Tenants.Commands;
using Application.Tenants.Queries;

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
}

public record CreateTenantRequest(string Name);
public record UpdateTenantRequest(string Name);
