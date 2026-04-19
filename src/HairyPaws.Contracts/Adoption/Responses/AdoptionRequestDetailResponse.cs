using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Contracts.Users.Responses;
using HairyPaws.Contracts.Visits.Responses;

namespace HairyPaws.Contracts.Adoption.Responses;

public sealed record AdoptionRequestDetailResponse(
    Guid Id,
    PetSummaryResponse Pet,
    UserSummaryResponse Adopter,
    string Status,
    string ContactPhone,
    string? LivingConditions,
    bool HasPreviousPets,
    string WhyAdopt,
    string? ReviewNotes,
    UserSummaryResponse? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    IReadOnlyCollection<VisitListItemResponse> Visits,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
