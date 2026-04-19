using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Organizations.Entities;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Visits.Entities;

namespace HairyPaws.Application.Common.Audit;

public static class AuditSnapshots
{
    public static object ToAuditSnapshot(this User user) => new
    {
        user.Id,
        user.Email,
        Role = user.Role.ToString(),
        Status = user.Status.ToString(),
        VerificationStatus = user.VerificationStatus.ToString(),
        user.FirstName,
        user.LastName,
        user.PhoneNumber,
        user.IdentityDocument,
        user.Address,
        user.ProfileImagePath,
        user.CreatedAt,
        user.UpdatedAt,
        user.DeletedAt
    };

    public static object ToAuditSnapshot(this Organization organization) => new
    {
        organization.Id,
        organization.OwnerUserId,
        organization.Name,
        organization.Ruc,
        organization.Description,
        organization.Address,
        organization.Phone,
        organization.Email,
        organization.LogoPath,
        VerificationStatus = organization.VerificationStatus.ToString(),
        organization.CreatedAt,
        organization.UpdatedAt,
        organization.DeletedAt
    };

    public static object ToAuditSnapshot(this OrganizationDocument document) => new
    {
        document.Id,
        document.OrganizationId,
        DocumentType = document.DocumentType.ToString(),
        document.FilePath,
        document.UploadedAt
    };

    public static object ToAuditSnapshot(this Pet pet) => new
    {
        pet.Id,
        pet.OwnerUserId,
        pet.OrganizationId,
        pet.Name,
        Species = pet.Species.ToString(),
        pet.Breed,
        pet.AgeText,
        Sex = pet.Sex.ToString(),
        Size = pet.Size.ToString(),
        pet.Sterilized,
        pet.Vaccinated,
        pet.Description,
        pet.Temperament,
        pet.MedicalHistory,
        pet.LocationDistrict,
        Status = pet.Status.ToString(),
        pet.PublishedAt,
        pet.CreatedAt,
        pet.UpdatedAt,
        pet.DeletedAt
    };

    public static object ToAuditSnapshot(this PetPhoto photo) => new
    {
        photo.Id,
        photo.PetId,
        photo.FilePath,
        photo.SortOrder,
        photo.CreatedAt
    };

    public static object ToAuditSnapshot(this AdoptionRequest adoptionRequest) => new
    {
        adoptionRequest.Id,
        adoptionRequest.PetId,
        adoptionRequest.AdopterUserId,
        Status = adoptionRequest.Status.ToString(),
        adoptionRequest.ContactPhone,
        adoptionRequest.LivingConditions,
        adoptionRequest.HasPreviousPets,
        adoptionRequest.WhyAdopt,
        adoptionRequest.ReviewNotes,
        adoptionRequest.ReviewedByUserId,
        adoptionRequest.ReviewedAt,
        adoptionRequest.CreatedAt,
        adoptionRequest.UpdatedAt
    };

    public static object ToAuditSnapshot(this Visit visit) => new
    {
        visit.Id,
        visit.AdoptionRequestId,
        visit.ScheduledAt,
        visit.Location,
        Status = visit.Status.ToString(),
        visit.Notes,
        visit.CreatedAt,
        visit.UpdatedAt
    };

    public static object ToAuditSnapshot(this Donation donation) => new
    {
        donation.Id,
        donation.DonorUserId,
        donation.OrganizationId,
        DonationType = donation.DonationType.ToString(),
        Status = donation.Status.ToString(),
        donation.Amount,
        donation.TransactionId,
        donation.Notes,
        donation.ReceiptPath,
        donation.ConfirmedByUserId,
        donation.ConfirmedAt,
        donation.CreatedAt,
        donation.UpdatedAt
    };

    public static object ToAuditSnapshot(this Event eventEntity) => new
    {
        eventEntity.Id,
        eventEntity.OrganizationId,
        eventEntity.Title,
        eventEntity.Description,
        eventEntity.EventDate,
        eventEntity.Location,
        eventEntity.IsVolunteerEvent,
        eventEntity.ImagePath,
        Status = eventEntity.Status.ToString(),
        eventEntity.CreatedAt,
        eventEntity.UpdatedAt,
        eventEntity.DeletedAt
    };
}
