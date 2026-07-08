using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Domain.Notifications.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Commands.CancelDonation;

public sealed record CancelDonationCommand(Guid DonationId);

public sealed class CancelDonationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    INotificationService notificationService,
    IAuditService auditService)
    : ICommandHandler<CancelDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(CancelDonationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var donation = await dbContext.Donations
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.DonationId, cancellationToken)
            ?? throw new NotFoundException("The donation was not found.");

        if (!await CurrentUserContext.CanManageDonationAsync(dbContext, actor, donation, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to cancel this donation.");
        }

        if (!donation.CanCancel())
        {
            throw new BusinessRuleViolationException("Only pending donations can be cancelled.");
        }

        var before = donation.ToAuditSnapshot();
        donation.Cancel(dateTimeProvider.UtcNow);
        await notificationService.CreateAsync(
            donation.DonorUserId,
            NotificationType.DonationCancelled,
            "Donation cancelled",
            $"Your donation to {donation.Organization.Name} was cancelled.",
            "Donation",
            donation.Id,
            cancellationToken);
        await auditService.WriteAsync(
            "Cancel",
            actor.Id,
            "Donation",
            donation.Id,
            before,
            donation.ToAuditSnapshot(),
            metadata: null,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await dbContext.Donations
            .AsNoTracking()
            .IncludeForDetail()
            .SingleAsync(entity => entity.Id == donation.Id, cancellationToken);

        return response.ToResponse();
    }
}
