using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using HairyPaws.Domain.Adoption.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.ApproveAdoptionRequest;

public sealed record ApproveAdoptionRequestCommand(Guid AdoptionRequestId, string? Notes);

public sealed class ApproveAdoptionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<ApproveAdoptionRequestCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(ApproveAdoptionRequestCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to approve this adoption request.");
        }

        if (!adoptionRequest.CanApprove())
        {
            throw new BusinessRuleViolationException("Only submitted or under review adoption requests can be approved.");
        }

        if (!adoptionRequest.Pet.CanMoveToPendingAdoption())
        {
            throw new BusinessRuleViolationException("Only available pets can move to pending adoption.");
        }

        var otherApprovedRequestExists = await dbContext.AdoptionRequests.AnyAsync(
            entity =>
                entity.PetId == adoptionRequest.PetId &&
                entity.Id != adoptionRequest.Id &&
                entity.Status == AdoptionRequestStatus.Approved,
            cancellationToken);

        if (otherApprovedRequestExists)
        {
            throw new ConflictException("This pet already has another approved adoption request.");
        }

        var competingRequests = await dbContext.AdoptionRequests
            .Include(entity => entity.Visits)
            .Where(entity =>
                entity.PetId == adoptionRequest.PetId &&
                entity.Id != adoptionRequest.Id &&
                (entity.Status == AdoptionRequestStatus.Submitted ||
                 entity.Status == AdoptionRequestStatus.UnderReview ||
                 entity.Status == AdoptionRequestStatus.Approved))
            .ToListAsync(cancellationToken);

        var before = adoptionRequest.ToAuditSnapshot();
        adoptionRequest.Approve(actor.Id, command.Notes, utcNow);
        adoptionRequest.Pet.MarkPendingAdoption(utcNow);

        foreach (var competingRequest in competingRequests)
        {
            competingRequest.AutoReject(
                actor.Id,
                "Automatically rejected because another adoption request was approved for this pet.",
                utcNow);

            competingRequest.CancelActiveVisits(
                "Visit cancelled because another adoption request was approved for this pet.",
                utcNow);
        }

        await auditService.WriteAsync(
            "Approve",
            actor.Id,
            "AdoptionRequest",
            adoptionRequest.Id,
            before,
            adoptionRequest.ToAuditSnapshot(),
            new
            {
                command.Notes,
                petStatus = adoptionRequest.Pet.Status.ToString(),
                resolvedCompetingRequestIds = competingRequests.Select(static request => request.Id).ToArray()
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.AdoptionRequests
            .AsNoTracking()
            .IncludeForDetail()
            .SingleAsync(entity => entity.Id == adoptionRequest.Id, cancellationToken);

        return response.ToDetailResponse();
    }
}
