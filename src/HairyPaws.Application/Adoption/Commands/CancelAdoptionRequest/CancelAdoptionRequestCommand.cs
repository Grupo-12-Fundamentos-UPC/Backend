using HairyPaws.Application.Adoption.Common;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Adoption.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Adoption.Commands.CancelAdoptionRequest;

public sealed record CancelAdoptionRequestCommand(Guid AdoptionRequestId, string? Notes);

public sealed class CancelAdoptionRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CancelAdoptionRequestCommand, AdoptionRequestDetailResponse>
{
    public async Task<AdoptionRequestDetailResponse> Handle(CancelAdoptionRequestCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var adoptionRequest = await dbContext.AdoptionRequests
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.AdoptionRequestId, cancellationToken)
            ?? throw new NotFoundException("The adoption request was not found.");

        if (!adoptionRequest.IsOwnedByAdopter(actor.Id))
        {
            throw new ForbiddenAppException("You are not allowed to cancel this adoption request.");
        }

        if (!adoptionRequest.CanCancelByAdopter())
        {
            throw new BusinessRuleViolationException("Only submitted or under review adoption requests can be cancelled.");
        }

        var before = adoptionRequest.ToAuditSnapshot();
        adoptionRequest.Cancel(command.Notes, dateTimeProvider.UtcNow);
        adoptionRequest.CancelActiveVisits("Visit cancelled because the adoption request was cancelled.", dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Cancel",
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
