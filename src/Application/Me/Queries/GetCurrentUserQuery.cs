using Application.Common.Mediator;

namespace Application.Me.Queries;

/// <summary>The caller's own account. Carries no user id: it is always taken from the caller's token.</summary>
public record GetCurrentUserQuery : IQuery<CurrentUserDto>;
