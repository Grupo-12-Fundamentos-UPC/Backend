namespace HairyPaws.Contracts.Organizations.Requests;

public sealed record UpdateOrganizationRequest
{
    public string? Name { get; init; }

    public string? Ruc { get; init; }

    public string? Description { get; init; }

    public string? Address { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }
}
