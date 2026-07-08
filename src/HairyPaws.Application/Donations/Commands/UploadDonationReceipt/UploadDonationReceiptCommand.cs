using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Files;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Domain.Identity.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Commands.UploadDonationReceipt;

public sealed record UploadDonationReceiptCommand(Guid DonationId, UploadedFile File);

public sealed class UploadDonationReceiptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService)
    : ICommandHandler<UploadDonationReceiptCommand, DonationResponse>
{
    private const long MaxReceiptSizeBytes = 10 * 1024 * 1024;

    public async Task<DonationResponse> Handle(UploadDonationReceiptCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var donation = await dbContext.Donations
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.DonationId, cancellationToken)
            ?? throw new NotFoundException("The donation was not found.");

        if (actor.Role != UserRole.Admin && !donation.IsOwnedByDonor(actor.Id))
        {
            throw new ForbiddenAppException("You are not allowed to manage this donation receipt.");
        }

        if (!donation.CanManageReceipt())
        {
            throw new BusinessRuleViolationException("Receipts can only be uploaded while the donation is pending.");
        }

        UploadedFileValidator.EnsureDocumentIsValid(command.File, "file", MaxReceiptSizeBytes);
        var extension = UploadedFileValidator.GetRequiredExtension(command.File, "file", ".pdf", ".jpg", ".jpeg", ".png");
        var relativePath = $"donations/receipts/{donation.Id}/{Guid.NewGuid():N}{extension}";
        var previousReceiptPath = donation.ReceiptPath;
        var before = donation.ToAuditSnapshot();

        if (!string.IsNullOrWhiteSpace(donation.ReceiptPath))
        {
            await fileStorageService.DeleteAsync(donation.ReceiptPath, cancellationToken);
        }

        await using var contentStream = command.File.OpenReadStream();
        var savedPath = await fileStorageService.SaveAsync(contentStream, relativePath, cancellationToken);

        donation.ReplaceReceipt(savedPath, dateTimeProvider.UtcNow);
        await auditService.WriteAsync(
            "UploadReceipt",
            actor.Id,
            "Donation",
            donation.Id,
            before,
            donation.ToAuditSnapshot(),
            new
            {
                previousReceiptPath,
                savedPath
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Donations
            .AsNoTracking()
            .IncludeForDetail()
            .SingleAsync(entity => entity.Id == donation.Id, cancellationToken);

        return response.ToResponse();
    }
}
