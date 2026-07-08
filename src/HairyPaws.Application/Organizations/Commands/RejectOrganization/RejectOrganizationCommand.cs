using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.RejectOrganization;

public sealed record RejectOrganizationCommand(Guid OrganizationId, string? Notes);

public sealed class RejectOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<RejectOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(RejectOrganizationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Admin)
        {
            throw new ForbiddenAppException("Only administrators can reject organizations.");
        }

        var organization = await dbContext.Organizations
            .Include(entity => entity.Documents)
            .SingleOrDefaultAsync(
                entity => entity.Id == command.OrganizationId && entity.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        var before = organization.ToAuditSnapshot();
        organization.UpdateVerificationStatus(VerificationStatus.Rejected, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Reject",
            actor.Id,
            "Organization",
            organization.Id,
            before,
            organization.ToAuditSnapshot(),
            new
            {
                command.Notes
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
