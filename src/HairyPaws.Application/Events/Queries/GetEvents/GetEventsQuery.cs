using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Queries.GetEvents;

public sealed record GetEventsQuery(
    int Page,
    int PageSize,
    string? Search,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    bool? IsVolunteerEvent,
    string? SortBy,
    string? SortDirection);

public sealed class GetEventsQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetEventsQuery, PagedResponse<EventListItemResponse>>
{
    public async Task<PagedResponse<EventListItemResponse>> Handle(GetEventsQuery query, CancellationToken cancellationToken)
    {
        var events = dbContext.Events
            .AsNoTracking()
            .IncludeForResponse()
            .Where(entity => entity.DeletedAt == null && entity.Status == EventStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            events = events.Where(entity =>
                entity.Title.ToLower().Contains(search) ||
                entity.Description.ToLower().Contains(search) ||
                (entity.Location != null && entity.Location.ToLower().Contains(search)));
        }

        if (query.FromDate.HasValue)
        {
            events = events.Where(entity => entity.EventDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            events = events.Where(entity => entity.EventDate <= query.ToDate.Value);
        }

        if (query.IsVolunteerEvent.HasValue)
        {
            events = events.Where(entity => entity.IsVolunteerEvent == query.IsVolunteerEvent.Value);
        }

        events = ApplySorting(events, query.SortBy, query.SortDirection);

        var totalCount = await events.LongCountAsync(cancellationToken);
        var items = await events
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResponse<EventListItemResponse>(
            items.Select(static entity => entity.ToListItemResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<Domain.Events.Entities.Event> ApplySorting(
        IQueryable<Domain.Events.Entities.Event> events,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "createdat" => descending
                ? events.OrderByDescending(entity => entity.CreatedAt)
                : events.OrderBy(entity => entity.CreatedAt),
            "title" => descending
                ? events.OrderByDescending(entity => entity.Title).ThenByDescending(entity => entity.EventDate)
                : events.OrderBy(entity => entity.Title).ThenBy(entity => entity.EventDate),
            _ => descending
                ? events.OrderByDescending(entity => entity.EventDate).ThenByDescending(entity => entity.CreatedAt)
                : events.OrderBy(entity => entity.EventDate).ThenBy(entity => entity.CreatedAt)
        };
    }
}
