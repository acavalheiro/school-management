namespace Application.Common.Interfaces;

/// <summary>
/// Resolves the authenticated caller's user id from the current request. Implemented by reading the
/// JWT <c>sub</c> claim off <c>IHttpContextAccessor</c>. Handlers that act on "my" account must take the
/// user id from here, never from the route or body, because Identity tables are unfiltered.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>The caller's user id, or <see cref="Guid.Empty"/> when it is missing or malformed.</summary>
    Guid UserId { get; }
}
