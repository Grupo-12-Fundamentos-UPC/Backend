using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid OrganizationId,
    string? Name,
    string? Ruc,
    string? Description,
    string? Address,
    string? Phone,
    string? Email);

public sealed class UpdateOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UpdateOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var organization = await dbContext.Organizations
            .Include(entity => entity.Documents)
            .SingleOrDefaultAsync(
                entity => entity.Id == command.OrganizationId && entity.DeletedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        if (!await CurrentUserContext.CanManageOrganizationAsync(dbContext, actor, organization, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to update this organization.");
        }

        var nextRuc = command.Ruc?.Trim() ?? organization.Ruc;
        if (!string.Equals(nextRuc, organization.Ruc, StringComparison.Ordinal))
        {
            var duplicateRucExists = await dbContext.Organizations.AnyAsync(
                entity => entity.Id != organization.Id && entity.Ruc == nextRuc && entity.DeletedAt == null,
                cancellationToken);

            if (duplicateRucExists)
            {
                throw new ConflictException("An organization with the same RUC already exists.");
            }
        }

        var before = organization.ToAuditSnapshot();
        organization.Update(
            command.Name ?? organization.Name,
            nextRuc,
            command.Description ?? organization.Description,
            command.Address ?? organization.Address,
            command.Phone ?? organization.Phone,
            command.Email ?? organization.Email,
            dateTimeProvider.UtcNow);

        await auditService.WriteAsync(
            "Update",
            actor.Id,
            "Organization",
            organization.Id,
            before,
            organization.ToAuditSnapshot(),
            metadata: null,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
