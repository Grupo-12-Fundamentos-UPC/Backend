using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.VerifyOrganization;

public sealed record VerifyOrganizationCommand(Guid OrganizationId, string? Notes);

public sealed class VerifyOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<VerifyOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(VerifyOrganizationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Admin)
        {
            throw new ForbiddenAppException("Only administrators can verify organizations.");
        }

        var organization = await dbContext.Organizations
            .Include(entity => entity.Documents)
            .SingleOrDefaultAsync(
                entity => entity.Id == command.OrganizationId && entity.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        var before = organization.ToAuditSnapshot();
        organization.UpdateVerificationStatus(VerificationStatus.Verified, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "Verify",
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
