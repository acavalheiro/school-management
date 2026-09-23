using Api;
using Application.Auth.Commands;
using Application.Common.Mediator;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Endpoints;

/// <summary>Anonymous registration and login endpoints. Rate-limited via <see cref="RateLimitPolicies.Auth"/>.</summary>
public static class AuthEndpoints
{
    /// <summary>Maps <c>POST /api/auth/register</c> and <c>POST /api/auth/login</c> under <c>/api/auth</c>.</summary>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitPolicies.Auth);

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

/// <summary>Creates a new <c>Tenant</c> named <paramref name="TenantName"/> and registers the caller as its sole Admin.</summary>
public record RegisterRequest(string Email, string Password, string ConfirmPassword, string TenantName);

/// <summary>Credentials for <c>POST /api/auth/login</c>.</summary>
public record LoginRequest(string Email, string Password);
