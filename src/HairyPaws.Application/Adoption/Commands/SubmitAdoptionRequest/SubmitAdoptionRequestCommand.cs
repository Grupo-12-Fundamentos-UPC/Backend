using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Adoption.Enums;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.SubmitAdoptionRequest;

public sealed record SubmitAdoptionRequestCommand(
    Guid PetId,
    string ContactPhone,
    string? LivingConditions,
    bool HasPreviousPets,
    string WhyAdopt);

public sealed class SubmitAdoptionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<SubmitAdoptionRequestCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(SubmitAdoptionRequestCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Adopter)
        {
            throw new ForbiddenAppException("Only users with role Adopter can submit adoption requests.");
        }

        var pet = await dbContext.Pets
            .Include(entity => entity.Photos)
            .SingleOrDefaultAsync(entity => entity.Id == command.PetId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The pet was not found.");

        if (!pet.CanReceiveAdoptionRequests())
        {
            throw new BusinessRuleViolationException("Only available pets can receive adoption requests.");
        }

        if (pet.OwnerUserId == actor.Id)
        {
            throw new BusinessRuleViolationException("You cannot submit an adoption request for your own pet.");
        }

        if (pet.OrganizationId.HasValue)
        {
            var ownsOrganization = await dbContext.Organizations.AnyAsync(
                organization => organization.Id == pet.OrganizationId.Value && organization.OwnerUserId == actor.Id,
                cancellationToken);

            if (ownsOrganization)
            {
                throw new BusinessRuleViolationException("You cannot submit an adoption request for your own pet.");
            }
        }

        var duplicateActiveRequestExists = await dbContext.AdoptionRequests.AnyAsync(
            adoptionRequest =>
                adoptionRequest.PetId == command.PetId &&
                adoptionRequest.AdopterUserId == actor.Id &&
                (adoptionRequest.Status == AdoptionRequestStatus.Submitted ||
                 adoptionRequest.Status == AdoptionRequestStatus.UnderReview ||
                 adoptionRequest.Status == AdoptionRequestStatus.Approved),
            cancellationToken);

        if (duplicateActiveRequestExists)
        {
            throw new ConflictException("An active adoption request already exists for this pet and adopter.");
        }

        var adoptionRequest = AdoptionRequest.Create(
            command.PetId,
            actor.Id,
            command.ContactPhone,
            command.LivingConditions,
            command.HasPreviousPets,
            command.WhyAdopt,
            dateTimeProvider.UtcNow);

        await dbContext.AdoptionRequests.AddAsync(adoptionRequest, cancellationToken);
        await auditService.WriteAsync(
            "Submit",
            actor.Id,
            "AdoptionRequest",
            adoptionRequest.Id,
            before: null,
            after: adoptionRequest.ToAuditSnapshot(),
            metadata: new
            {
                adoptionRequest.PetId
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleAsync(entity => entity.Id == adoptionRequest.Id, cancellationToken);

        return response.ToDetailResponse();
    }
}
