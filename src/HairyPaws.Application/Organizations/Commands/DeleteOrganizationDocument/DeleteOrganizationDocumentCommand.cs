using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Organizations.Commands.DeleteOrganizationDocument;

public sealed record DeleteOrganizationDocumentCommand(Guid OrganizationId, Guid DocumentId);

public sealed class DeleteOrganizationDocumentCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<DeleteOrganizationDocumentCommand>
{
    public async Task Handle(DeleteOrganizationDocumentCommand command, CancellationToken cancellationToken)
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

        var document = organization.Documents.SingleOrDefault(entity => entity.Id == command.DocumentId)
            ?? throw new NotFoundException("The organization document was not found.");

        var before = organization.ToAuditSnapshot();
        var removedDocument = document.ToAuditSnapshot();
        organization.RemoveDocument(document, dateTimeProvider.UtcNow);
        dbContext.OrganizationDocuments.Remove(document);
        await auditService.WriteAsync(
            "DeleteDocument",
            actor.Id,
            "Organization",
            organization.Id,
            before,
            organization.ToAuditSnapshot(),
            removedDocument,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorageService.DeleteAsync(document.FilePath, cancellationToken);
    }
}
