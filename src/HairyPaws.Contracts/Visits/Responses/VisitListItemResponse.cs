namespace HairyPaws.Contracts.Visits.Responses;

public sealed record VisitListItemResponse(
    Guid Id,
    Guid AdoptionRequestId,
    DateTimeOffset ScheduledAt,
    string? Location,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
