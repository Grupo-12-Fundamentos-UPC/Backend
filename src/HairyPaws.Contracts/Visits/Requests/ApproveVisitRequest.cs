namespace HairyPaws.Contracts.Visits.Requests;

public sealed record ApproveVisitRequest
{
    public string? Notes { get; init; }
}
