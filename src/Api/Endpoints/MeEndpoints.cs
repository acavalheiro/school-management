using Application.Common.Mediator;
using Application.Me.Commands;
using Application.Me.Queries;

namespace Api.Endpoints;

/// <summary>
/// The caller's own account under <c>/api/me</c>. Any authenticated role. Every operation acts on the
/// user in the caller's token; no endpoint here accepts a user id.
/// </summary>
public static class MeEndpoints
{
    /// <summary>Maps the current-user endpoints under <c>/api/me</c>.</summary>
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/me")
            .WithTags("Me")
            .RequireAuthorization();

        group.MapGet("/", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get the signed-in user's account, including their preferred language");

        group.MapPut("/language", UpdateLanguage)
            .WithName("UpdateMyLanguage")
            .WithSummary("Change the signed-in user's preferred language (en-GB or pt-PT)");
    }

    private static async Task<IResult> GetCurrentUser(IMediator mediator, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Description, statusCode: 404);
    }

    private static async Task<IResult> UpdateLanguage(
        UpdateMyLanguageRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UpdateMyLanguageCommand(request.Language), ct);
        if (result.IsSuccess)
            return Results.NoContent();

        return result.Error.Code.EndsWith(".NotFound", StringComparison.Ordinal)
            ? Results.Problem(result.Error.Description, statusCode: 404)
            : Results.Problem(result.Error.Description, statusCode: 400);
    }
}

/// <summary>The new UI language. Must be one of <c>SupportedLanguages.All</c>: <c>en-GB</c> or <c>pt-PT</c>.</summary>
public record UpdateMyLanguageRequest(string Language);
