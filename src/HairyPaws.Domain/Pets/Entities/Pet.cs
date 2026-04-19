using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Organizations.Entities;
using HairyPaws.Domain.Pets.Enums;

namespace HairyPaws.Domain.Pets.Entities;

public sealed class Pet : AuditableEntity
{
    private readonly List<PetPhoto> _photos = [];

    private Pet()
    {
    }

    private Pet(
        Guid? ownerUserId,
        Guid? organizationId,
        string? name,
        PetSpecies species,
        string? breed,
        string? ageText,
        PetSex sex,
        PetSize size,
        bool sterilized,
        bool vaccinated,
        string? description,
        string? temperament,
        string? medicalHistory,
        string? locationDistrict,
        DateTimeOffset utcNow)
    {
        if ((ownerUserId is null && organizationId is null) || (ownerUserId is not null && organizationId is not null))
        {
            throw new InvalidOperationException("A pet must belong either to an owner user or to an organization.");
        }

        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        OrganizationId = organizationId;
        Name = NormalizeOptional(name);
        Species = species;
        Breed = NormalizeOptional(breed);
        AgeText = NormalizeOptional(ageText);
        Sex = sex;
        Size = size;
        Sterilized = sterilized;
        Vaccinated = vaccinated;
        Description = NormalizeOptional(description);
        Temperament = NormalizeOptional(temperament);
        MedicalHistory = NormalizeOptional(medicalHistory);
        LocationDistrict = NormalizeOptional(locationDistrict);
        Status = PetStatus.Draft;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public Guid? OwnerUserId { get; private set; }

    public User? OwnerUser { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public Organization? Organization { get; private set; }

    public string? Name { get; private set; }

    public PetSpecies Species { get; private set; }

    public string? Breed { get; private set; }

    public string? AgeText { get; private set; }

    public PetSex Sex { get; private set; }

    public PetSize Size { get; private set; }

    public bool Sterilized { get; private set; }

    public bool Vaccinated { get; private set; }

    public string? Description { get; private set; }

    public string? Temperament { get; private set; }

    public string? MedicalHistory { get; private set; }

    public string? LocationDistrict { get; private set; }

    public PetStatus Status { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public IReadOnlyCollection<PetPhoto> Photos => _photos;

    public static Pet CreateForOwner(
        Guid ownerUserId,
        string? name,
        PetSpecies species,
        string? breed,
        string? ageText,
        PetSex sex,
        PetSize size,
        bool sterilized,
        bool vaccinated,
        string? description,
        string? temperament,
        string? medicalHistory,
        string? locationDistrict,
        DateTimeOffset utcNow)
    {
        return new Pet(
            ownerUserId,
            null,
            name,
            species,
            breed,
            ageText,
            sex,
            size,
            sterilized,
            vaccinated,
            description,
            temperament,
            medicalHistory,
            locationDistrict,
            utcNow);
    }

    public static Pet CreateForOrganization(
        Guid organizationId,
        string? name,
        PetSpecies species,
        string? breed,
        string? ageText,
        PetSex sex,
        PetSize size,
        bool sterilized,
        bool vaccinated,
        string? description,
        string? temperament,
        string? medicalHistory,
        string? locationDistrict,
        DateTimeOffset utcNow)
    {
        return new Pet(
            null,
            organizationId,
            name,
            species,
            breed,
            ageText,
            sex,
            size,
            sterilized,
            vaccinated,
            description,
            temperament,
            medicalHistory,
            locationDistrict,
            utcNow);
    }

    public bool IsPubliclyVisible() => DeletedAt is null && Status == PetStatus.Available;

    public bool CanReceiveAdoptionRequests() => DeletedAt is null && Status == PetStatus.Available;

    public bool CanMoveToPendingAdoption() => DeletedAt is null && Status == PetStatus.Available;

    public bool CanMoveToAdopted() => DeletedAt is null && Status == PetStatus.PendingAdoption;

    public bool IsOwnedByUser(Guid userId) => OwnerUserId == userId;

    public bool BelongsToOrganization(Guid organizationId) => OrganizationId == organizationId;

    public IReadOnlyCollection<string> GetPublishValidationErrors(int photoCount)
    {
        List<string> errors = [];

        if (!Enum.IsDefined(Species))
        {
            errors.Add("Species must be defined before publishing.");
        }

        if (Sex == PetSex.Unknown)
        {
            errors.Add("Sex must be defined before publishing.");
        }

        if (Size == PetSize.Unknown)
        {
            errors.Add("Size must be defined before publishing.");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            errors.Add("Description is required before publishing.");
        }

        if (string.IsNullOrWhiteSpace(LocationDistrict))
        {
            errors.Add("Location district is required before publishing.");
        }

        if (photoCount <= 0)
        {
            errors.Add("At least one photo is required before publishing.");
        }

        return errors;
    }

    public void UpdateDetails(
        string? name,
        PetSpecies species,
        string? breed,
        string? ageText,
        PetSex sex,
        PetSize size,
        bool sterilized,
        bool vaccinated,
        string? description,
        string? temperament,
        string? medicalHistory,
        string? locationDistrict,
        DateTimeOffset utcNow)
    {
        Name = NormalizeOptional(name);
        Species = species;
        Breed = NormalizeOptional(breed);
        AgeText = NormalizeOptional(ageText);
        Sex = sex;
        Size = size;
        Sterilized = sterilized;
        Vaccinated = vaccinated;
        Description = NormalizeOptional(description);
        Temperament = NormalizeOptional(temperament);
        MedicalHistory = NormalizeOptional(medicalHistory);
        LocationDistrict = NormalizeOptional(locationDistrict);
        UpdatedAt = utcNow;
    }

    public PetPhoto AddPhoto(string filePath, int sortOrder, DateTimeOffset utcNow)
    {
        var photo = PetPhoto.Create(Id, filePath, sortOrder, utcNow);
        _photos.Add(photo);
        UpdatedAt = utcNow;
        return photo;
    }

    public void RemovePhoto(PetPhoto photo, DateTimeOffset utcNow)
    {
        _photos.Remove(photo);
        UpdatedAt = utcNow;
    }

    public void Publish(DateTimeOffset utcNow)
    {
        Status = PetStatus.Available;
        PublishedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void MarkPendingAdoption(DateTimeOffset utcNow)
    {
        Status = PetStatus.PendingAdoption;
        UpdatedAt = utcNow;
    }

    public void MarkAdopted(DateTimeOffset utcNow)
    {
        Status = PetStatus.Adopted;
        UpdatedAt = utcNow;
    }

    public void Archive(DateTimeOffset utcNow)
    {
        Status = PetStatus.Archived;
        UpdatedAt = utcNow;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
