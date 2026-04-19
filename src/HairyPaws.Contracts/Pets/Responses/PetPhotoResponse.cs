namespace HairyPaws.Contracts.Pets.Responses;

public sealed record PetPhotoResponse(
    Guid Id,
    string FilePath,
    int SortOrder,
    DateTimeOffset CreatedAt);
