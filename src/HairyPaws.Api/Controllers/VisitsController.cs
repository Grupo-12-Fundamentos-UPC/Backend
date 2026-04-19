using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Visits.Commands.ApproveVisit;
using HairyPaws.Application.Visits.Commands.CancelVisit;
using HairyPaws.Application.Visits.Commands.CompleteVisit;
using HairyPaws.Application.Visits.Commands.RejectVisit;
using HairyPaws.Application.Visits.Queries.GetVisitById;
using HairyPaws.Contracts.Visits.Requests;
using HairyPaws.Contracts.Visits.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
[Route("api/v1/visits")]
public sealed class VisitsController : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetVisitByIdQuery, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetVisitByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitResponse>> Approve(
        Guid id,
        [FromBody] ApproveVisitRequest request,
        [FromServices] ICommandHandler<ApproveVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new ApproveVisitCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitResponse>> Reject(
        Guid id,
        [FromBody] RejectVisitRequest request,
        [FromServices] ICommandHandler<RejectVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new RejectVisitCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitResponse>> Cancel(
        Guid id,
        [FromBody] CancelVisitRequest request,
        [FromServices] ICommandHandler<CancelVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new CancelVisitCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(VisitResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<VisitResponse>> Complete(
        Guid id,
        [FromBody] CompleteVisitRequest request,
        [FromServices] ICommandHandler<CompleteVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new CompleteVisitCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }
}
