using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Visits.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Commands.CreateVisit;

public sealed record CreateVisitCommand(
    Guid AdoptionRequestId,
    DateTimeOffset ScheduledAt,
    string? Location,
    string? Notes);

public sealed class CreateVisitCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CreateVisitCommand, VisitResponse>
{
    public async Task<VisitResponse> Handle(CreateVisitCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!await CurrentUserContext.CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to create visits for this adoption request.");
        }

        if (!adoptionRequest.CanCreateVisit())
        {
            throw new BusinessRuleViolationException("Visits can only be created for adoption requests under review.");
        }

        if (adoptionRequest.Visits.Any(static visit => visit.HasActiveStatus()))
        {
            throw new ConflictException("Only one active visit is allowed per adoption request.");
        }

        var visit = adoptionRequest.AddVisit(command.ScheduledAt, command.Location, command.Notes, dateTimeProvider.UtcNow);
        await dbContext.Visits.AddAsync(visit, cancellationToken);
        await auditService.WriteAsync(
            "Create",
            actor.Id,
            "Visit",
            visit.Id,
            before: null,
            after: visit.ToAuditSnapshot(),
            metadata: new
            {
                visit.AdoptionRequestId
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
