namespace HairyPaws.Contracts.Organizations.Requests;

public sealed record RejectOrganizationRequest
{
    public string? Notes { get; init; }
}
