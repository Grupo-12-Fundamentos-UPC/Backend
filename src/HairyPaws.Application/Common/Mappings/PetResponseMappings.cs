using HairyPaws.Contracts.Pets.Responses;
using HairyPaws.Domain.Pets.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class PetResponseMappings
{
    public static PetListItemResponse ToListItemResponse(this Pet pet)
    {
        return new PetListItemResponse(
            pet.Id,
            pet.Name,
            pet.Species.ToString(),
            pet.Breed,
            pet.AgeText,
            pet.Sex.ToString(),
            pet.Size.ToString(),
            pet.Sterilized,
            pet.Vaccinated,
            pet.Description,
            pet.LocationDistrict,
            pet.Status.ToString(),
            pet.PublishedAt,
            pet.Photos
                .OrderBy(photo => photo.SortOrder)
                .Select(photo => photo.FilePath)
                .FirstOrDefault());
    }

    public static PetSummaryResponse ToSummaryResponse(this Pet pet)
    {
        return new PetSummaryResponse(
            pet.Id,
            pet.Name,
            pet.Species.ToString(),
            pet.Status.ToString(),
            pet.Photos
                .OrderBy(photo => photo.SortOrder)
                .Select(photo => photo.FilePath)
                .FirstOrDefault());
    }

    public static PetDetailResponse ToDetailResponse(this Pet pet)
    {
        return new PetDetailResponse(
            pet.Id,
            pet.Name,
            pet.Species.ToString(),
            pet.Breed,
            pet.AgeText,
            pet.Sex.ToString(),
            pet.Size.ToString(),
            pet.Sterilized,
            pet.Vaccinated,
            pet.Description,
            pet.Temperament,
            pet.MedicalHistory,
            pet.LocationDistrict,
            pet.Status.ToString(),
            pet.PublishedAt,
            pet.Photos
                .OrderBy(photo => photo.SortOrder)
                .Select(static photo => photo.ToResponse())
                .ToArray(),
            pet.CreatedAt,
            pet.UpdatedAt);
    }

    public static PetPhotoResponse ToResponse(this PetPhoto photo)
    {
        return new PetPhotoResponse(
            photo.Id,
            photo.FilePath,
            photo.SortOrder,
            photo.CreatedAt);
    }
}
