using System.Text;

namespace Infrastructure.Identity;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    // HMAC-SHA256 keys shorter than the 256-bit hash add no security and are
    // rejected outright by the token handler.
    private const int MinimumSecretBytes = 32;

    private static readonly string[] KnownPlaceholders =
    [
        "change-this-to-a-secure-random-secret-at-least-32-chars"
    ];

    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; } = 60;

    /// <summary>
    /// Fails startup rather than letting the app run on a guessable signing key.
    /// Anyone holding the key can mint a token for any tenant and any role, so a
    /// weak or committed secret is a total compromise of tenant isolation.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            throw new InvalidOperationException(
                $"{SectionName}:Secret is not configured. {HowToConfigure}");

        if (KnownPlaceholders.Contains(Secret))
            throw new InvalidOperationException(
                $"{SectionName}:Secret is still the placeholder value from source control. {HowToConfigure}");

        if (Encoding.UTF8.GetByteCount(Secret) < MinimumSecretBytes)
            throw new InvalidOperationException(
                $"{SectionName}:Secret must be at least {MinimumSecretBytes} bytes. {HowToConfigure}");

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException(
                $"{SectionName}:Issuer and {SectionName}:Audience must both be configured.");
    }

    private const string HowToConfigure =
        "Set it outside source control — locally: " +
        "dotnet user-secrets set \"JwtSettings:Secret\" \"<random 32+ byte value>\" --project src/Api";
}
