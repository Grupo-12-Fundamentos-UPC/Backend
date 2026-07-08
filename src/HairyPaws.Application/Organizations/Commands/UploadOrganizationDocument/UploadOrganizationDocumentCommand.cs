using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Files;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Contracts.Organizations.Responses;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.UploadOrganizationDocument;

public sealed record UploadOrganizationDocumentCommand(
    Guid OrganizationId,
    string DocumentType,
    UploadedFile File);

public sealed class UploadOrganizationDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UploadOrganizationDocumentCommand, OrganizationDocumentResponse>
{
    private const long MaxDocumentSizeBytes = 10 * 1024 * 1024;

    public async Task<OrganizationDocumentResponse> Handle(UploadOrganizationDocumentCommand command, CancellationToken cancellationToken)
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
            throw new ForbiddenAppException("You are not allowed to manage this organization's documents.");
        }

        UploadedFileValidator.EnsureDocumentIsValid(command.File, "file", MaxDocumentSizeBytes);
        var extension = UploadedFileValidator.GetRequiredExtension(command.File, "file", ".pdf", ".jpg", ".jpeg", ".png");
        var documentType = ContractEnumMapper.ToOrganizationDocumentType(command.DocumentType);
        var relativePath = $"organizations/documents/{organization.Id}/{Guid.NewGuid():N}{extension}";

        await using var contentStream = command.File.OpenReadStream();
        var savedPath = await fileStorageService.SaveAsync(contentStream, relativePath, cancellationToken);

        var before = organization.ToAuditSnapshot();
        var document = organization.AddDocument(documentType, savedPath, dateTimeProvider.UtcNow);
        await dbContext.OrganizationDocuments.AddAsync(document, cancellationToken);
        await auditService.WriteAsync(
            "UploadDocument",
            actor.Id,
            "Organization",
            organization.Id,
            before,
            organization.ToAuditSnapshot(),
            document.ToAuditSnapshot(),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return document.ToResponse();
    }
}
