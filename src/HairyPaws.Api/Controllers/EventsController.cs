using HairyPaws.Api.Common.Extensions;
using HairyPaws.Api.Models;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Events.Commands.CancelEvent;
using HairyPaws.Application.Events.Commands.CreateEvent;
using HairyPaws.Application.Events.Commands.PublishEvent;
using HairyPaws.Application.Events.Commands.UpdateEvent;
using HairyPaws.Application.Events.Commands.UploadEventImage;
using HairyPaws.Application.Events.Queries.GetEventById;
using HairyPaws.Application.Events.Queries.GetEvents;
using HairyPaws.Application.Events.Queries.GetMyEvents;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Events.Requests;
using HairyPaws.Contracts.Events.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Route("api/v1/events")]
public sealed class EventsController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<EventDetailResponse>> Create(
        [FromBody] CreateEventRequest request,
        [FromServices] ICommandHandler<CreateEventCommand, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CreateEventCommand(
                request.OrganizationId,
                request.Title,
                request.Description,
                request.EventDate,
                request.Location,
                request.IsVolunteerEvent),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        [FromServices] ICommandHandler<UpdateEventCommand, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new UpdateEventCommand(
                id,
                request.Title,
                request.Description,
                request.EventDate,
                request.Location,
                request.IsVolunteerEvent),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EventListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EventListItemResponse>>> GetEvents(
        [FromQuery] EventsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetEventsQuery, PagedResponse<EventListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetEventsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Search,
                queryParameters.FromDate,
                queryParameters.ToDate,
                queryParameters.IsVolunteerEvent,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetEventByIdQuery, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetEventByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResponse<EventListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EventListItemResponse>>> GetMine(
        [FromQuery] MyEventsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetMyEventsQuery, PagedResponse<EventListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetMyEventsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.Search,
                queryParameters.FromDate,
                queryParameters.ToDate,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDetailResponse>> UploadImage(
        Guid id,
        [FromForm] UploadFileRequest request,
        [FromServices] ICommandHandler<UploadEventImageCommand, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var file = await request.File.ToUploadedFileAsync("file", cancellationToken);
        var response = await handler.Handle(new UploadEventImageCommand(id, file), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDetailResponse>> Publish(
        Guid id,
        [FromBody] PublishEventRequest request,
        [FromServices] ICommandHandler<PublishEventCommand, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        _ = request;
        var response = await handler.Handle(new PublishEventCommand(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDetailResponse>> Cancel(
        Guid id,
        [FromBody] CancelEventRequest request,
        [FromServices] ICommandHandler<CancelEventCommand, EventDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        _ = request;
        var response = await handler.Handle(new CancelEventCommand(id), cancellationToken);
        return Ok(response);
    }
}
