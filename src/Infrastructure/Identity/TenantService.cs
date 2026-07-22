using Application.Common;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.Identity;

internal sealed class TenantService(IHttpContextAccessor httpContextAccessor) : ITenantService
{
    public Guid TenantId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?
                .User.FindFirstValue("tid");

            // An unparseable claim yields Guid.Empty, which matches no rows. It must
            // never be treated as "see everything" — see CanBypassTenantFilter.
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public bool CanBypassTenantFilter =>
        httpContextAccessor.HttpContext?.User.IsInRole(AppRoles.SuperAdmin) ?? false;
}
