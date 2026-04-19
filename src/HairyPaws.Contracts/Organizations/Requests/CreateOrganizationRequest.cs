namespace HairyPaws.Contracts.Organizations.Requests;

public sealed record CreateOrganizationRequest
{
    public string Name { get; init; } = string.Empty;

    public string Ruc { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Address { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }
}
