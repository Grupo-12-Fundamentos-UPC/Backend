namespace HairyPaws.Contracts.Visits.Requests;

public sealed record CreateVisitRequest
{
    public DateTimeOffset ScheduledAt { get; init; }

    public string? Location { get; init; }

    public string? Notes { get; init; }
}
