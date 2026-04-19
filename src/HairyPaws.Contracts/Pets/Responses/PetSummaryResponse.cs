namespace HairyPaws.Contracts.Pets.Responses;

public sealed record PetSummaryResponse(
    Guid Id,
    string? Name,
    string Species,
    string Status,
    string? PrimaryPhotoPath);
