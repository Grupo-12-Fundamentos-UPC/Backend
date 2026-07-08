using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.RejectAdoptionRequest;

public sealed record RejectAdoptionRequestCommand(Guid AdoptionRequestId, string? Notes);

public sealed class RejectAdoptionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<RejectAdoptionRequestCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(RejectAdoptionRequestCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to reject this adoption request.");
        }

        if (!adoptionRequest.CanReject())
        {
            throw new BusinessRuleViolationException("Only submitted or under review adoption requests can be rejected.");
        }

        var before = adoptionRequest.ToAuditSnapshot();
        adoptionRequest.Reject(actor.Id, command.Notes, dateTimeProvider.UtcNow);
        adoptionRequest.CancelActiveVisits("Visit cancelled because the adoption request was rejected.", dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Reject",
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
