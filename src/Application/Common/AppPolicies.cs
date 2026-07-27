namespace Application.Common;

public static class AppPolicies
{
    // Admin (own tenant) or SuperAdmin (explicit tenant) may create students.
    public const string StudentWrite = "StudentWrite";
}
