using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Visits.Responses;
using HairyPaws.Domain.Adoption.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Visits.Commands.ApproveVisit;

public sealed record ApproveVisitCommand(Guid VisitId, string? Notes);

public sealed class ApproveVisitCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<ApproveVisitCommand, VisitResponse>
{
    public async Task<VisitResponse> Handle(ApproveVisitCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var visit = await dbContext.Visits
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.VisitId, cancellationToken)
            ?? throw new NotFoundException("The visit was not found.");

        if (!visit.AdoptionRequest.IsOwnedByAdopter(actor.Id))
        {
            throw new ForbiddenAppException("Only the adopter who owns the adoption request can approve this visit.");
        }

        if (!visit.CanBeApprovedByAdopter())
        {
            throw new BusinessRuleViolationException("Only pending visits can be approved.");
        }

        if (visit.AdoptionRequest.Status is not (AdoptionRequestStatus.UnderReview or AdoptionRequestStatus.Approved))
        {
            throw new BusinessRuleViolationException("The visit can only be approved while the adoption request is active.");
        }

        var before = visit.ToAuditSnapshot();
        visit.Approve(command.Notes, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Approve",
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
