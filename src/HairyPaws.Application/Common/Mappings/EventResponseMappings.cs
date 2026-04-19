using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Domain.Events.Entities;

namespace HairyPaws.Application.Common.Mappings;

public static class EventResponseMappings
{
    public static EventListItemResponse ToListItemResponse(this Event eventEntity)
    {
        return new EventListItemResponse(
            eventEntity.Id,
            eventEntity.Organization.ToSummaryResponse(),
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.IsVolunteerEvent,
            eventEntity.ImagePath,
            eventEntity.Status.ToString(),
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt);
    }

    public static EventDetailResponse ToDetailResponse(this Event eventEntity)
    {
        return new EventDetailResponse(
            eventEntity.Id,
            eventEntity.Organization.ToSummaryResponse(),
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Location,
            eventEntity.IsVolunteerEvent,
            eventEntity.ImagePath,
            eventEntity.Status.ToString(),
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt);
    }
}
