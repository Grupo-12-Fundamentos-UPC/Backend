namespace HairyPaws.Contracts.Adoption.Requests;

public sealed record StartAdoptionReviewRequest
{
    public string? Notes { get; init; }
}
