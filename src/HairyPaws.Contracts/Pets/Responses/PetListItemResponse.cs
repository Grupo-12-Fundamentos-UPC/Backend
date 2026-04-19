namespace HairyPaws.Contracts.Pets.Responses;

public sealed record PetListItemResponse(
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
    string? LocationDistrict,
    string Status,
    DateTimeOffset? PublishedAt,
    string? PrimaryPhotoPath);
