using HairyPaws.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Common;

internal static class EventQueryExtensions
{
    public static IQueryable<Event> IncludeForResponse(this IQueryable<Event> query)
    {
        return query.Include(eventEntity => eventEntity.Organization);
    }
}
