using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Visits.Responses;
using HairyPaws.Domain.Adoption.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Commands.CompleteVisit;

public sealed record CompleteVisitCommand(Guid VisitId, string? Notes);

public sealed class CompleteVisitCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CompleteVisitCommand, VisitResponse>
{
    public async Task<VisitResponse> Handle(CompleteVisitCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var visit = await dbContext.Visits
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.VisitId, cancellationToken)
            ?? throw new NotFoundException("The visit was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, visit.AdoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to complete this visit.");
        }

        if (!visit.CanBeCompletedByManager())
        {
            throw new BusinessRuleViolationException("Only approved visits can be completed.");
        }

        if (visit.AdoptionRequest.Status is not (AdoptionRequestStatus.UnderReview or AdoptionRequestStatus.Approved))
        {
            throw new BusinessRuleViolationException("Only active adoption requests can have completed visits.");
        }

        var before = visit.ToAuditSnapshot();
        visit.Complete(command.Notes, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Complete",
            actor.Id,
            "Visit",
            visit.Id,
            before,
            visit.ToAuditSnapshot(),
            new
            {
                command.Notes
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Visits
            .AsNoTracking()
            .IncludeForDetail()
            .SingleAsync(entity => entity.Id == visit.Id, cancellationToken);

        return response.ToResponse();
    }
}
