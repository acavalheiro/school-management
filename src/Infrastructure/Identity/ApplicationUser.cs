using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    // Optional link to a Student record
    public Guid? StudentId { get; set; }

    public Guid TenantId { get; set; }
}
