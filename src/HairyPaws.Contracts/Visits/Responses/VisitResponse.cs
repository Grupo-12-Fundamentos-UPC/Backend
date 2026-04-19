using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Contracts.Users.Responses;

namespace HairyPaws.Contracts.Visits.Responses;

public sealed record VisitResponse(
    Guid Id,
    Guid AdoptionRequestId,
    PetSummaryResponse Pet,
    UserSummaryResponse Adopter,
    DateTimeOffset ScheduledAt,
    string? Location,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
