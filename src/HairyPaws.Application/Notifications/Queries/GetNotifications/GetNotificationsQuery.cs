using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Notifications.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    int Page,
    int PageSize,
    bool? IsRead,
    string? Type,
    string? SortBy,
    string? SortDirection);

public sealed class GetNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetNotificationsQuery, PagedResponse<NotificationResponse>>
{
    public async Task<PagedResponse<NotificationResponse>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var notifications = dbContext.Notifications
            .AsNoTracking()
            .Where(entity => entity.UserId == actor.Id);

        if (query.IsRead.HasValue)
        {
            notifications = notifications.Where(entity => entity.IsRead == query.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = ContractEnumMapper.ToNotificationType(query.Type);
            notifications = notifications.Where(entity => entity.Type == type);
        }

        notifications = ApplySorting(notifications, query.SortBy, query.SortDirection);

        var totalCount = await notifications.LongCountAsync(cancellationToken);
        var items = await notifications
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PagedResponse<NotificationResponse>(
            items.Select(static entity => entity.ToResponse()).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static IQueryable<Domain.Notifications.Entities.Notification> ApplySorting(
        IQueryable<Domain.Notifications.Entities.Notification> notifications,
        string? sortBy,
        string? sortDirection)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return normalizedSortBy switch
        {
            "type" => descending
                ? notifications.OrderByDescending(entity => entity.Type).ThenByDescending(entity => entity.CreatedAt)
                : notifications.OrderBy(entity => entity.Type).ThenBy(entity => entity.CreatedAt),
            "isread" => descending
                ? notifications.OrderByDescending(entity => entity.IsRead).ThenByDescending(entity => entity.CreatedAt)
                : notifications.OrderBy(entity => entity.IsRead).ThenBy(entity => entity.CreatedAt),
            _ => descending
                ? notifications.OrderByDescending(entity => entity.CreatedAt)
                : notifications.OrderBy(entity => entity.CreatedAt)
        };
    }
}
