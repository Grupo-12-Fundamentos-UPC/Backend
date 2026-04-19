using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Contracts.Users.Responses;

namespace HairyPaws.Contracts.Adoption.Responses;

public sealed record AdoptionRequestListItemResponse(
    Guid Id,
    PetSummaryResponse Pet,
    UserSummaryResponse Adopter,
    string Status,
    string ContactPhone,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
