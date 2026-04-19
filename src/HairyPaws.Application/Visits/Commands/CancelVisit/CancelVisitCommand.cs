using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Visits.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Commands.CancelVisit;

public sealed record CancelVisitCommand(Guid VisitId, string? Notes);

public sealed class CancelVisitCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CancelVisitCommand, VisitResponse>
{
    public async Task<VisitResponse> Handle(CancelVisitCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var visit = await dbContext.Visits
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.VisitId, cancellationToken)
            ?? throw new NotFoundException("The visit was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, visit.AdoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to cancel this visit.");
        }

        if (!visit.CanBeCancelledByManager())
        {
            throw new BusinessRuleViolationException("Only pending or approved visits can be cancelled.");
        }

        var before = visit.ToAuditSnapshot();
        visit.Cancel(command.Notes, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Cancel",
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
