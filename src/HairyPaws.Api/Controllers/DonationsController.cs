using HairyPaws.Api.Common.Extensions;
using HairyPaws.Api.Models;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Commands.CancelDonation;
using HairyPaws.Application.Donations.Commands.ConfirmDonation;
using HairyPaws.Application.Donations.Commands.CreateDonation;
using HairyPaws.Application.Donations.Commands.UploadDonationReceipt;
using HairyPaws.Application.Donations.Queries.GetDonationById;
using HairyPaws.Application.Donations.Queries.GetMyDonations;
using HairyPaws.Application.Donations.Queries.GetOrganizationDonations;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Contracts.Donations.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
[Route("api/v1")]
public sealed class DonationsController : ControllerBase
{
    [HttpPost("donations")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DonationResponse>> Create(
        [FromBody] CreateDonationRequest request,
        [FromServices] ICommandHandler<CreateDonationCommand, DonationResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CreateDonationCommand(
                request.OrganizationId,
                request.DonationType,
                request.Amount,
                request.TransactionId,
                request.Notes,
                request.Items),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("donations/my")]
    [ProducesResponseType(typeof(PagedResponse<DonationListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DonationListItemResponse>>> GetMyDonations(
        [FromQuery] DonationsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetMyDonationsQuery, PagedResponse<DonationListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetMyDonationsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.DonationType,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("donations/{id:guid}")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DonationResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetDonationByIdQuery, DonationResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetDonationByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("donations/{id:guid}/receipt")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DonationResponse>> UploadReceipt(
        Guid id,
        [FromForm] UploadFileRequest request,
        [FromServices] ICommandHandler<UploadDonationReceiptCommand, DonationResponse> handler,
        CancellationToken cancellationToken)
    {
        var file = await request.File.ToUploadedFileAsync("file", cancellationToken);
        var response = await handler.Handle(new UploadDonationReceiptCommand(id, file), cancellationToken);
        return Ok(response);
    }

    [HttpGet("organizations/me/donations")]
    [ProducesResponseType(typeof(PagedResponse<DonationListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DonationListItemResponse>>> GetOrganizationDonations(
        [FromQuery] OrganizationDonationsQueryParameters queryParameters,
        [FromServices] IQueryHandler<GetOrganizationDonationsQuery, PagedResponse<DonationListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetOrganizationDonationsQuery(
                queryParameters.Page,
                queryParameters.PageSize,
                queryParameters.Status,
                queryParameters.DonationType,
                queryParameters.Search,
                queryParameters.SortBy,
                queryParameters.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("donations/{id:guid}/confirm")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DonationResponse>> Confirm(
        Guid id,
        [FromBody] ConfirmDonationRequest request,
        [FromServices] ICommandHandler<ConfirmDonationCommand, DonationResponse> handler,
        CancellationToken cancellationToken)
    {
        _ = request;
        var response = await handler.Handle(new ConfirmDonationCommand(id), cancellationToken);
        return Ok(response);
    }

    [HttpPost("donations/{id:guid}/cancel")]
    [ProducesResponseType(typeof(DonationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DonationResponse>> Cancel(
        Guid id,
        [FromBody] CancelDonationRequest request,
        [FromServices] ICommandHandler<CancelDonationCommand, DonationResponse> handler,
        CancellationToken cancellationToken)
    {
        _ = request;
        var response = await handler.Handle(new CancelDonationCommand(id), cancellationToken);
        return Ok(response);
    }
}
