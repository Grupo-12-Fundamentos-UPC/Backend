using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Notifications.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Notifications.Queries.GetUnreadNotificationsCount;

public sealed record GetUnreadNotificationsCountQuery;

public sealed class GetUnreadNotificationsCountQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUnreadNotificationsCountQuery, UnreadNotificationsCountResponse>
{
    public async Task<UnreadNotificationsCountResponse> Handle(GetUnreadNotificationsCountQuery query, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var count = await dbContext.Notifications.CountAsync(
            entity => entity.UserId == actor.Id && !entity.IsRead,
            cancellationToken);

        return new UnreadNotificationsCountResponse(count);
    }
}
