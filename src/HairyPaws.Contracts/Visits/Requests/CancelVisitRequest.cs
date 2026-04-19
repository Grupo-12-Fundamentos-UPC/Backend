namespace HairyPaws.Contracts.Visits.Requests;

public sealed record CancelVisitRequest
{
    public string? Notes { get; init; }
}
