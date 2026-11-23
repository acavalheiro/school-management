using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Identity;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            // MapInboundClaims is off, so the JWT's `sub` arrives under its own name.
            var value = httpContextAccessor.HttpContext?
                .User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            // Guid.Empty matches no user, so a missing or malformed claim fails closed.
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }
}
