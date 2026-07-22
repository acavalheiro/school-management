using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Auth.Queries;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

internal sealed class TokenService(IOptions<JwtSettings> options)
{
    private readonly JwtSettings _settings = options.Value;

    /// <summary>The claim carrying the user's Identity security stamp.</summary>
    public const string SecurityStampClaim = "sstamp";

    public AuthTokenDto GenerateToken(
        Guid userId, string email, IEnumerable<string> roles, Guid tenantId, string securityStamp)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tid", tenantId.ToString()),
            new(SecurityStampClaim, securityStamp)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new AuthTokenDto(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
