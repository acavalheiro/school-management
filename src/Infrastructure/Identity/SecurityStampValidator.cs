using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity;

/// <summary>
/// JWTs are self-contained and cannot be withdrawn once issued, so deleting a user
/// or changing their role would otherwise leave a valid token in circulation until
/// it expired. Comparing the token's security stamp against the stored one on every
/// request gives us revocation: any call to UpdateSecurityStampAsync (or deleting
/// the user) invalidates every token already issued to them.
///
/// The cost is one user lookup per authenticated request — the right trade for data
/// about minors, but worth knowing about.
/// </summary>
public static class SecurityStampValidation
{
    public static JwtBearerEvents CreateEvents() => new()
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var userId = principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            var tokenStamp = principal?.FindFirstValue(TokenService.SecurityStampClaim);

            if (userId is null || tokenStamp is null)
            {
                context.Fail("Token is missing the subject or security stamp claim.");
                return;
            }

            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                // Deleted account: the token is still cryptographically valid.
                context.Fail("The account no longer exists.");
                return;
            }

            var currentStamp = await userManager.GetSecurityStampAsync(user);
            if (!string.Equals(currentStamp, tokenStamp, StringComparison.Ordinal))
            {
                services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(SecurityStampValidation))
                    .LogInformation("Rejected a revoked token for user {UserId}", user.Id);

                context.Fail("The token has been revoked.");
            }
        }
    };
}
