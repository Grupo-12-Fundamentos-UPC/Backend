using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Organizations.Commands.RejectOrganization;
using HairyPaws.Application.Organizations.Commands.VerifyOrganization;
using HairyPaws.Application.Organizations.Queries.GetPendingOrganizations;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[Route("api/v1/admin/organizations")]
public sealed class AdminOrganizationsController : ControllerBase
{
    [HttpGet("pending-review")]
    [ProducesResponseType(typeof(PagedResponse<OrganizationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrganizationSummaryResponse>>> GetPendingReview(
        [FromQuery] GetPendingOrganizationsQueryParameters request,
        [FromServices] IQueryHandler<GetPendingOrganizationsQuery, PagedResponse<OrganizationSummaryResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetPendingOrganizationsQuery(request.Page, request.PageSize, request.Search),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> Verify(
        Guid id,
        [FromBody] VerifyOrganizationRequest request,
        [FromServices] ICommandHandler<VerifyOrganizationCommand, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new VerifyOrganizationCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> Reject(
        Guid id,
        [FromBody] RejectOrganizationRequest request,
        [FromServices] ICommandHandler<RejectOrganizationCommand, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new RejectOrganizationCommand(id, request.Notes), cancellationToken);
        return Ok(response);
    }
}
