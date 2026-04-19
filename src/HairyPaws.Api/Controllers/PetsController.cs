using HairyPaws.Api.Common.Extensions;
using HairyPaws.Api.Models;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Pets.Commands.ArchivePet;
using HairyPaws.Application.Pets.Commands.CreatePet;
using HairyPaws.Application.Pets.Commands.DeletePetPhoto;
using HairyPaws.Application.Pets.Commands.PublishPet;
using HairyPaws.Application.Pets.Commands.UpdatePet;
using HairyPaws.Application.Pets.Commands.UploadPetPhoto;
using HairyPaws.Application.Pets.Queries.GetMyPets;
using HairyPaws.Application.Pets.Queries.GetPetById;
using HairyPaws.Application.Pets.Queries.GetPetsCatalog;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Contracts.Pets.Requests;
using HairyPaws.Contracts.Pets.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HairyPaws.Api.Controllers;

[ApiController]
[Route("api/v1/pets")]
public sealed class PetsController : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.RequireOwnerOrOng)]
    [HttpPost]
    [ProducesResponseType(typeof(PetDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PetDetailResponse>> Create(
        [FromBody] CreatePetRequest request,
        [FromServices] ICommandHandler<CreatePetCommand, PetDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new CreatePetCommand(
                request.Name,
                request.Species,
                request.Breed,
                request.AgeText,
                request.Sex,
                request.Size,
                request.Sterilized,
                request.Vaccinated,
                request.Description,
                request.Temperament,
                request.MedicalHistory,
                request.LocationDistrict),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PetDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PetDetailResponse>> Update(
        Guid id,
        [FromBody] UpdatePetRequest request,
        [FromServices] ICommandHandler<UpdatePetCommand, PetDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new UpdatePetCommand(
                id,
                request.Name,
                request.Species,
                request.Breed,
                request.AgeText,
                request.Sex,
                request.Size,
                request.Sterilized,
                request.Vaccinated,
                request.Description,
                request.Temperament,
                request.MedicalHistory,
                request.LocationDistrict),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PetDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PetDetailResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetPetByIdQuery, PetDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetPetByIdQuery(id), cancellationToken);
        return Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PetListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PetListItemResponse>>> GetCatalog(
        [FromQuery] PetCatalogQueryParameters request,
        [FromServices] IQueryHandler<GetPetsCatalogQuery, PagedResponse<PetListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new GetPetsCatalogQuery(
                request.Page,
                request.PageSize,
                request.Species,
                request.Sex,
                request.Size,
                request.LocationDistrict,
                request.Search,
                request.SortBy,
                request.SortDirection),
            cancellationToken);

        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireOwnerOrOng)]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PetListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PetListItemResponse>>> Mine(
        [FromServices] IQueryHandler<GetMyPetsQuery, IReadOnlyCollection<PetListItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new GetMyPetsQuery(), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PetPhotoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PetPhotoResponse>> UploadPhoto(
        Guid id,
        [FromForm] UploadFileRequest request,
        [FromServices] ICommandHandler<UploadPetPhotoCommand, PetPhotoResponse> handler,
        CancellationToken cancellationToken)
    {
        var file = await request.File.ToUploadedFileAsync("file", cancellationToken);
        var response = await handler.Handle(new UploadPetPhotoCommand(id, file), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePhoto(
        Guid id,
        Guid photoId,
        [FromServices] ICommandHandler<DeletePetPhotoCommand> handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new DeletePetPhotoCommand(id, photoId), cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PetDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PetDetailResponse>> Publish(
        Guid id,
        [FromServices] ICommandHandler<PublishPetCommand, PetDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new PublishPetCommand(id), cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(PetDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PetDetailResponse>> Archive(
        Guid id,
        [FromServices] ICommandHandler<ArchivePetCommand, PetDetailResponse> handler,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new ArchivePetCommand(id), cancellationToken);
        return Ok(response);
    }
}
