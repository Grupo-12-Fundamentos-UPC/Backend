namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record CancelAdoptionRequestRequest
{
    public string? Notes { get; init; }
}
