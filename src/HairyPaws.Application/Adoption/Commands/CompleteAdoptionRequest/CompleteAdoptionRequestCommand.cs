using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.CompleteAdoptionRequest;

public sealed record CompleteAdoptionRequestCommand(Guid AdoptionRequestId, string? Notes);

public sealed class CompleteAdoptionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CompleteAdoptionRequestCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(CompleteAdoptionRequestCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var utcNow = dateTimeProvider.UtcNow;

        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to complete this adoption request.");
        }

        if (!adoptionRequest.CanComplete())
        {
            throw new BusinessRuleViolationException("Only approved adoption requests can be completed.");
        }

        if (!adoptionRequest.Pet.CanMoveToAdopted())
        {
            throw new BusinessRuleViolationException("Only pets in pending adoption can become adopted.");
        }

        var before = adoptionRequest.ToAuditSnapshot();
        adoptionRequest.Complete(actor.Id, command.Notes, utcNow);
        adoptionRequest.CancelActiveVisits("Visit cancelled because the adoption process was completed.", utcNow);
        adoptionRequest.Pet.MarkAdopted(utcNow);
        await auditService.WriteAsync(
            "Complete",
            actor.Id,
            "AdoptionRequest",
            adoptionRequest.Id,
            before,
            adoptionRequest.ToAuditSnapshot(),
            new
            {
                command.Notes,
                petStatus = adoptionRequest.Pet.Status.ToString()
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
