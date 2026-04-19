using HairyPaws.Application.Adoption.Commands.ApproveAdoptionRequest;
using HairyPaws.Application.Adoption.Commands.CancelAdoptionRequest;
using HairyPaws.Application.Adoption.Commands.CompleteAdoptionRequest;
using HairyPaws.Application.Adoption.Commands.RejectAdoptionRequest;
using HairyPaws.Application.Adoption.Commands.StartAdoptionReview;
using HairyPaws.Application.Adoption.Commands.SubmitAdoptionRequest;
using HairyPaws.Application.Adoption.Queries.GetAdoptionRequestById;
using HairyPaws.Application.Adoption.Queries.GetMyAdoptionRequests;
using HairyPaws.Application.Adoption.Queries.GetPetAdoptionRequests;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Visits.Commands.CreateVisit;
using HairyPaws.Application.Visits.Queries.GetVisitsByAdoptionRequest;
using HairyPaws.Contracts.Adoption.Requests;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Visits.Requests;
using HairyPaws.Contracts.Visits.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
[Route("api/v1")]
public sealed class AdoptionRequestsController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.RequireAdopter)]
    [HttpPost("adoption-requests")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> Submit(
        [FromBody] SubmitAdoptionRequestRequest request,
        [FromServices] ICommandHandler<SubmitAdoptionRequestCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new SubmitAdoptionRequestCommand(
                request.PetId,
                request.ContactPhone,
                request.LivingConditions,
                request.HasPreviousPets,
                request.WhyAdopt),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAdopter)]
    [HttpGet("adoption-requests/my")]
    [ProducesResponseType(typeof(PagedResponse<AdoptionRequestListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdoptionRequestListItemResponse>>> GetMyRequests(
        [FromQuery] AdoptionRequestsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetMyAdoptionRequestsQuery, PagedResponse<AdoptionRequestListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetMyAdoptionRequestsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("adoption-requests/{id:guid}")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetAdoptionRequestByIdQuery, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetAdoptionRequestByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet("pets/{petId:guid}/adoption-requests")]
    [ProducesResponseType(typeof(PagedResponse<AdoptionRequestListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdoptionRequestListItemResponse>>> GetByPet(
        Guid petId,
        [FromQuery] PetAdoptionRequestsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetPetAdoptionRequestsQuery, PagedResponse<AdoptionRequestListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetPetAdoptionRequestsQuery(
                petId,
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.Search,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/start-review")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> StartReview(
        Guid id,
        [FromBody] StartAdoptionReviewRequest request,
        [FromServices] ICommandHandler<StartAdoptionReviewCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new StartAdoptionReviewCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/approve")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> Approve(
        Guid id,
        [FromBody] ApproveAdoptionRequestRequest request,
        [FromServices] ICommandHandler<ApproveAdoptionRequestCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new ApproveAdoptionRequestCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/reject")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> Reject(
        Guid id,
        [FromBody] RejectAdoptionRequestRequest request,
        [FromServices] ICommandHandler<RejectAdoptionRequestCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new RejectAdoptionRequestCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/cancel")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> Cancel(
        Guid id,
        [FromBody] CancelAdoptionRequestRequest request,
        [FromServices] ICommandHandler<CancelAdoptionRequestCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new CancelAdoptionRequestCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/complete")]
    [ProducesResponseType(typeof(AdoptionRequestDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdoptionRequestDetailResponse>> Complete(
        Guid id,
        [FromBody] CompleteAdoptionRequestRequest request,
        [FromServices] ICommandHandler<CompleteAdoptionRequestCommand, AdoptionRequestDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new CompleteAdoptionRequestCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("adoption-requests/{id:guid}/visits")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<VisitResponse>> CreateVisit(
        Guid id,
        [FromBody] CreateVisitRequest request,
        [FromServices] ICommandHandler<CreateVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CreateVisitCommand(id, request.ScheduledAt, request.Location, request.Notes),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("adoption-requests/{id:guid}/visits")]
    [ProducesResponseType(typeof(PagedResponse<VisitListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VisitListItemResponse>>> GetVisits(
        Guid id,
        [FromQuery] VisitsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetVisitsByAdoptionRequestQuery, PagedResponse<VisitListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetVisitsByAdoptionRequestQuery(
                id,
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }
}
