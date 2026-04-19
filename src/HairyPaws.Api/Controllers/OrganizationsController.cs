using HairyPaws.Api.Common.Extensions;
using HairyPaws.Api.Models;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Organizations.Commands.CreateOrganization;
using HairyPaws.Application.Organizations.Commands.DeleteOrganizationDocument;
using HairyPaws.Application.Organizations.Commands.UpdateOrganization;
using HairyPaws.Application.Organizations.Commands.UploadOrganizationDocument;
using HairyPaws.Application.Organizations.Commands.UploadOrganizationLogo;
using HairyPaws.Application.Organizations.Queries.GetMyOrganization;
using HairyPaws.Application.Organizations.Queries.GetOrganizationById;
using HairyPaws.Contracts.Organizations.Requests;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Route("api/v1/organizations")]
public sealed class OrganizationsController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.RequireOng)]
    [HttpPost]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrganizationDetailResponse>> Create(
        [FromBody] CreateOrganizationRequest request,
        [FromServices] ICommandHandler<CreateOrganizationCommand, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CreateOrganizationCommand(
                request.Name,
                request.Ruc,
                request.Description,
                request.Address,
                request.Phone,
                request.Email),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpGet("me")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> Me(
        [FromServices] IQueryHandler<GetMyOrganizationQuery, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetMyOrganizationQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetOrganizationByIdQuery, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetOrganizationByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> Update(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        [FromServices] ICommandHandler<UpdateOrganizationCommand, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new UpdateOrganizationCommand(
                id,
                request.Name,
                request.Ruc,
                request.Description,
                request.Address,
                request.Phone,
                request.Email),
            cancellationToken);

        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/logo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OrganizationDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDetailResponse>> UploadLogo(
        Guid id,
        [FromForm] UploadFileRequest request,
        [FromServices] ICommandHandler<UploadOrganizationLogoCommand, OrganizationDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var file = await request.File.ToUploadedFileAsync("file", cancellationToken);
        var response = await handler.Handle(new UploadOrganizationLogoCommand(id, file), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(OrganizationDocumentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDocumentResponse>> UploadDocument(
        Guid id,
        [FromForm] UploadOrganizationDocumentRequest request,
        [FromServices] ICommandHandler<UploadOrganizationDocumentCommand, OrganizationDocumentResponse> handler,
        CancellationToken cancellationToken)
    {
        var file = await request.File.ToUploadedFileAsync("file", cancellationToken);
        var response = await handler.Handle(
            new UploadOrganizationDocumentCommand(id, request.DocumentType, file),
            cancellationToken);

        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        Guid documentId,
        [FromServices] ICommandHandler<DeleteOrganizationDocumentCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new DeleteOrganizationDocumentCommand(id, documentId), cancellationToken);
        return NoContent();
    }
}
