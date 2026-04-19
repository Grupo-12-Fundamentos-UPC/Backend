namespace HairyPaws.Contracts.Visits.Requests;

public sealed record RejectVisitRequest
{
    public string? Notes { get; init; }
}
