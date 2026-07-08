using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Files;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.UploadOrganizationLogo;

public sealed record UploadOrganizationLogoCommand(Guid OrganizationId, UploadedFile File);

public sealed class UploadOrganizationLogoCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UploadOrganizationLogoCommand, OrganizationDetailResponse>
{
    private const long MaxLogoSizeBytes = 5 * 1024 * 1024;

    public async Task<OrganizationDetailResponse> Handle(UploadOrganizationLogoCommand command, CancellationToken cancellationToken)
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
            throw new ForbiddenAppException("You are not allowed to manage this organization's logo.");
        }

        UploadedFileValidator.EnsureImageIsValid(command.File, "file", MaxLogoSizeBytes);
        var extension = UploadedFileValidator.GetRequiredExtension(command.File, "file", ".jpg", ".jpeg", ".png");
        var relativePath = $"organizations/logos/{organization.Id}/{Guid.NewGuid():N}{extension}";

        await using var contentStream = command.File.OpenReadStream();
        var savedPath = await fileStorageService.SaveAsync(contentStream, relativePath, cancellationToken);
        var previousLogoPath = organization.LogoPath;
        var before = organization.ToAuditSnapshot();

        organization.SetLogo(savedPath, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "UploadLogo",
            actor.Id,
            "Organization",
            organization.Id,
            before,
            organization.ToAuditSnapshot(),
            new
            {
                previousLogoPath,
                savedPath
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousLogoPath) &&
            !string.Equals(previousLogoPath, savedPath, StringComparison.OrdinalIgnoreCase))
        {
            await fileStorageService.DeleteAsync(previousLogoPath, cancellationToken);
        }

        return organization.ToDetailResponse(includeDocuments: true);
    }
}
