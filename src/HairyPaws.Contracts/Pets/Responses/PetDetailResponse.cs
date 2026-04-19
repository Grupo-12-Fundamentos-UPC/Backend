namespace HairyPaws.Contracts.Pets.Responses;

public sealed record PetDetailResponse(
    Guid Id,
    string? Name,
    string Species,
    string? Breed,
    string? AgeText,
    string Sex,
    string Size,
    bool Sterilized,
    bool Vaccinated,
    string? Description,
    string? Temperament,
    string? MedicalHistory,
    string? LocationDistrict,
    string Status,
    DateTimeOffset? PublishedAt,
    IReadOnlyCollection<PetPhotoResponse> Photos,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
