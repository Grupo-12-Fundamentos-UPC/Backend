using System.ComponentModel.DataAnnotations;

namespace HairyPaws.Infrastructure.Auth;

public sealed class AdminSeedOptions
{
    public const string SectionName = "Seed:AdminUser";

    [Required]
    [EmailAddress]
    public string Email { get; init; } = "admin@hairypaws.local";

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = "Admin123!";

    [Required]
    public string FirstName { get; init; } = "System";

    [Required]
    public string LastName { get; init; } = "Administrator";
}
