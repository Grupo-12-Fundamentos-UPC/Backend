using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Common;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Events.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Events.Queries.GetMyEvents;

public sealed record GetMyEventsQuery(
    int Page,
    int PageSize,
    string? Status,
    string? Search,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    string? SortBy,
    string? SortDirection);

public sealed class GetMyEventsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyEventsQuery, PagedResponse<EventListItemResponse>>
{
    public async Task<PagedResponse<EventListItemResponse>> Handle(GetMyEventsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);

        IQueryable<Domain.Events.Entities.Event> events = dbContext.Events
            .AsNoTracking()
            .IncludeForResponse()
            .Where(entity => entity.DeletedAt == null);

        if (actor.Role == UserRole.Admin)
        {
            // Admin can view all events.
        }
        else if (actor.Role == UserRole.Ong)
        {
            var organizationId = await CurrentUserContext.GetOwnedOrganizationIdAsync(dbContext, actor.Id, cancellationToken);
            if (!organizationId.HasValue)
            {
                return new PagedResponse<EventListItemResponse>([], query.Page, query.PageSize, 0, 0);
            }

            events = events.Where(entity => entity.OrganizationId == organizationId.Value);
        }
        else
        {
            throw new ForbiddenAppException("Only organization owners or admins can access events in this scope.");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ContractEnumMapper.ToEventStatus(query.Status);
            events = events.Where(entity => entity.Status == status);
        }

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
            "status" => descending
                ? events.OrderByDescending(entity => entity.Status).ThenByDescending(entity => entity.EventDate)
                : events.OrderBy(entity => entity.Status).ThenBy(entity => entity.EventDate),
            "title" => descending
                ? events.OrderByDescending(entity => entity.Title).ThenByDescending(entity => entity.EventDate)
                : events.OrderBy(entity => entity.Title).ThenBy(entity => entity.EventDate),
            _ => descending
                ? events.OrderByDescending(entity => entity.EventDate).ThenByDescending(entity => entity.CreatedAt)
                : events.OrderBy(entity => entity.EventDate).ThenBy(entity => entity.CreatedAt)
        };
    }
}
