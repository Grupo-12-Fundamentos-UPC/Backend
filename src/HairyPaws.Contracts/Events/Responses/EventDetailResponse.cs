using HairyPaws.Contracts.Organizations.Responses;

namespace HairyPaws.Contracts.Events.Responses;

public sealed record EventDetailResponse(
    Guid Id,
    OrganizationSummaryResponse Organization,
    string Title,
    string Description,
    DateTimeOffset EventDate,
    string? Location,
    bool IsVolunteerEvent,
    string? ImagePath,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
