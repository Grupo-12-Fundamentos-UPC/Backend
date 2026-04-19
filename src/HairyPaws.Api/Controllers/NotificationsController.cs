using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Notifications.Commands.MarkAllNotificationsAsRead;
using HairyPaws.Application.Notifications.Commands.MarkNotificationAsRead;
using HairyPaws.Application.Notifications.Queries.GetNotifications;
using HairyPaws.Application.Notifications.Queries.GetUnreadNotificationsCount;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Notifications.Requests;
using HairyPaws.Contracts.Notifications.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetNotifications(
        [FromQuery] NotificationsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetNotificationsQuery, PagedResponse<NotificationResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetNotificationsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.IsRead,
                queryParameters.Type,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationsCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationsCountResponse>> GetUnreadCount(
        [FromServices] IQueryHandler<GetUnreadNotificationsCountQuery, UnreadNotificationsCountResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetUnreadNotificationsCountQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationResponse>> MarkAsRead(
        Guid id,
        [FromServices] ICommandHandler<MarkNotificationAsReadCommand, NotificationResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new MarkNotificationAsReadCommand(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(
        [FromServices] ICommandHandler<MarkAllNotificationsAsReadCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return NoContent();
    }
}
