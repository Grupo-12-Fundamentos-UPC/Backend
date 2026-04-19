namespace HairyPaws.Infrastructure.Auth;

public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:AdminUser";

    public string Email { get; init; } = "admin@hairypaws.local";

    public string Password { get; init; } = "Admin123!";

    public string FirstName { get; init; } = "System";

    public string LastName { get; init; } = "Administrator";
}
