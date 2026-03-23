namespace Infrastructure.Identity;

public sealed class SuperAdminSettings
{
    public const string SectionName = "SuperAdminSettings";

    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
