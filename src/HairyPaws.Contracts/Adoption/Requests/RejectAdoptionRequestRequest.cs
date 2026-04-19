namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record RejectAdoptionRequestRequest
{
    public string? Notes { get; init; }
}
