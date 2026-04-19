namespace HairyPaws.Application.Common.Security;

public static class AuthorizationPolicies
{
    public const string RequireAuthenticatedUser = nameof(RequireAuthenticatedUser);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireAdopter = nameof(RequireAdopter);
    public const string RequireOng = nameof(RequireOng);
    public const string RequireOwnerOrOng = nameof(RequireOwnerOrOng);
}
