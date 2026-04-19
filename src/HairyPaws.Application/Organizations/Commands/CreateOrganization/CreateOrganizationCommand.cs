using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Organizations.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string Ruc,
    string? Description,
    string? Address,
    string? Phone,
    string? Email);

public sealed class CreateOrganizationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<CreateOrganizationCommand, OrganizationDetailResponse>
{
    public async Task<OrganizationDetailResponse> Handle(CreateOrganizationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        if (actor.Role != UserRole.Ong)
        {
            throw new ForbiddenAppException("Only users with role Ong can create organizations.");
        }

        var organizationAlreadyExists = await dbContext.Organizations.AnyAsync(
            organization => organization.OwnerUserId == actor.Id && organization.DeletedAt == null,
            cancellationToken);

        if (organizationAlreadyExists)
        {
            throw new ConflictException("The current user already owns an organization.");
        }

        var normalizedRuc = command.Ruc.Trim();
        var duplicateRucExists = await dbContext.Organizations.AnyAsync(
            organization => organization.Ruc == normalizedRuc && organization.DeletedAt == null,
            cancellationToken);

        if (duplicateRucExists)
        {
            throw new ConflictException("An organization with the same RUC already exists.");
        }

        var organization = Organization.Create(
            actor.Id,
            command.Name,
            normalizedRuc,
            dateTimeProvider.UtcNow,
            command.Description,
            command.Address,
            command.Phone,
            command.Email);

        await dbContext.Organizations.AddAsync(organization, cancellationToken);
        await auditService.WriteAsync(
            "Create",
            actor.Id,
            "Organization",
            organization.Id,
            before: null,
            after: organization.ToAuditSnapshot(),
            metadata: new
            {
                organization.OwnerUserId
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
