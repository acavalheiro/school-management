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

            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }
}
