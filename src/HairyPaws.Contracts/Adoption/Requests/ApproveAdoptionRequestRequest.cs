namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record ApproveAdoptionRequestRequest
{
    public string? Notes { get; init; }
}
