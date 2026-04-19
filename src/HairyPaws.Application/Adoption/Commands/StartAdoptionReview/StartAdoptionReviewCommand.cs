using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.StartAdoptionReview;

public sealed record StartAdoptionReviewCommand(Guid AdoptionRequestId, string? Notes);

public sealed class StartAdoptionReviewCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<StartAdoptionReviewCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(StartAdoptionReviewCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to review this adoption request.");
        }

        if (!adoptionRequest.CanStartReview())
        {
            throw new BusinessRuleViolationException("Only submitted adoption requests can move to under review.");
        }

        if (!adoptionRequest.Pet.CanReceiveAdoptionRequests())
        {
            throw new BusinessRuleViolationException("Only available pets can keep adoption requests under review.");
        }

        var before = adoptionRequest.ToAuditSnapshot();
        adoptionRequest.StartReview(actor.Id, command.Notes, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "StartReview",
            actor.Id,
            "AdoptionRequest",
            adoptionRequest.Id,
            before,
            adoptionRequest.ToAuditSnapshot(),
            new
            {
                command.Notes
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
