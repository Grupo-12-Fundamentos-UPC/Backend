namespace HairyPaws.Contracts.Organizations.Requests;

public sealed record VerifyOrganizationRequest
{
    public string? Notes { get; init; }
}
