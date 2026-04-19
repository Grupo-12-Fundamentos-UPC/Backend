namespace HairyPaws.Contracts.Visits.Requests;

public sealed record CompleteVisitRequest
{
    public string? Notes { get; init; }
}
