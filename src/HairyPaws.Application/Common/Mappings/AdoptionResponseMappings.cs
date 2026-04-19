using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Visits.Responses;
using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Visits.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class AdoptionResponseMappings
{
    public static AdoptionRequestListItemResponse ToListItemResponse(this AdoptionRequest adoptionRequest)
    {
        return new AdoptionRequestListItemResponse(
            adoptionRequest.Id,
            adoptionRequest.Pet.ToSummaryResponse(),
            adoptionRequest.AdopterUser.ToSummaryResponse(),
            adoptionRequest.Status.ToString(),
            adoptionRequest.ContactPhone,
            adoptionRequest.ReviewedAt,
            adoptionRequest.CreatedAt,
            adoptionRequest.UpdatedAt);
    }

    public static AdoptionRequestDetailResponse ToDetailResponse(this AdoptionRequest adoptionRequest)
    {
        return new AdoptionRequestDetailResponse(
            adoptionRequest.Id,
            adoptionRequest.Pet.ToSummaryResponse(),
            adoptionRequest.AdopterUser.ToSummaryResponse(),
            adoptionRequest.Status.ToString(),
            adoptionRequest.ContactPhone,
            adoptionRequest.LivingConditions,
            adoptionRequest.HasPreviousPets,
            adoptionRequest.WhyAdopt,
            adoptionRequest.ReviewNotes,
            adoptionRequest.ReviewedByUser?.ToSummaryResponse(),
            adoptionRequest.ReviewedAt,
            adoptionRequest.Visits
                .OrderBy(visit => visit.ScheduledAt)
                .Select(static visit => visit.ToListItemResponse())
                .ToArray(),
            adoptionRequest.CreatedAt,
            adoptionRequest.UpdatedAt);
    }

    public static VisitListItemResponse ToListItemResponse(this Visit visit)
    {
        return new VisitListItemResponse(
            visit.Id,
            visit.AdoptionRequestId,
            visit.ScheduledAt,
            visit.Location,
            visit.Status.ToString(),
            visit.Notes,
            visit.CreatedAt,
            visit.UpdatedAt);
    }

    public static VisitResponse ToResponse(this Visit visit)
    {
        return new VisitResponse(
            visit.Id,
            visit.AdoptionRequestId,
            visit.AdoptionRequest.Pet.ToSummaryResponse(),
            visit.AdoptionRequest.AdopterUser.ToSummaryResponse(),
            visit.ScheduledAt,
            visit.Location,
            visit.Status.ToString(),
            visit.Notes,
            visit.CreatedAt,
            visit.UpdatedAt);
    }
}
