using Application.Auth.Commands;
using Application.Common.Mediator;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Login and receive a JWT bearer token")
            .AllowAnonymous();
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.ConfirmPassword, request.TenantName);
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Results.Ok(new { userId = result.Value })
            : Results.Problem(result.Error.Description, statusCode: 400);
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 401);
    }
}

public record RegisterRequest(string Email, string Password, string ConfirmPassword, string TenantName);
public record LoginRequest(string Email, string Password);
