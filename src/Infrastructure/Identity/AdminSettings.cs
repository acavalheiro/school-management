namespace Infrastructure.Identity;

public sealed class AdminSettings
{
    public const string SectionName = "AdminSettings";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
}
