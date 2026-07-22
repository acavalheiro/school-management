namespace Application.Common;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";

    // Roles an Admin may assign within their own tenant.
    // SuperAdmin is deliberately excluded — it grants cross-tenant access and
    // must never be reachable through /api/users.
    public static readonly string[] Assignable = [Admin, User];

    public static bool IsAssignable(string role) => Assignable.Contains(role);
}
