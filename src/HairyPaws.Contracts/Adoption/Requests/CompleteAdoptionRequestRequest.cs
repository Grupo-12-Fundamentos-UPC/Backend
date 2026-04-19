namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record CompleteAdoptionRequestRequest
{
    public string? Notes { get; init; }
}
