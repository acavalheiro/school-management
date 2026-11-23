namespace Application.Common;

/// <summary>
/// The UI languages a user can choose. This list is authoritative: a language is only usable once
/// its translation catalogue ships in the frontend, so the set is defined in code rather than in a
/// table. Mirrored by <c>src/ui/src/i18n/languages.ts</c>.
/// </summary>
public static class SupportedLanguages
{
    public const string EnGb = "en-GB";
    public const string PtPt = "pt-PT";

    public const string Default = EnGb;

    public static readonly string[] All = [EnGb, PtPt];

    /// <summary>Exact, case-sensitive match against <see cref="All"/>.</summary>
    public static bool IsSupported(string? language) =>
        language is not null && All.Contains(language, StringComparer.Ordinal);
}
