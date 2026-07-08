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

namespace HairyPaws.Application.Donations.Commands.ConfirmDonation;

public sealed record ConfirmDonationCommand(Guid DonationId);

public sealed class ConfirmDonationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    INotificationService notificationService,
    IAuditService auditService)
    : ICommandHandler<ConfirmDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(ConfirmDonationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var donation = await dbContext.Donations
            .IncludeForDetail()
            .SingleOrDefaultAsync(entity => entity.Id == command.DonationId, cancellationToken)
            ?? throw new NotFoundException("The donation was not found.");

        if (!await CurrentUserContext.CanManageDonationAsync(dbContext, actor, donation, cancellationToken))
        {
            throw new ForbiddenAppException("You are not allowed to confirm this donation.");
        }

        if (!donation.CanConfirm())
        {
            throw new BusinessRuleViolationException("Only pending donations can be confirmed.");
        }

        var before = donation.ToAuditSnapshot();
        donation.Confirm(actor.Id, dateTimeProvider.UtcNow);
        await notificationService.CreateAsync(
            donation.DonorUserId,
            NotificationType.DonationConfirmed,
            "Donation confirmed",
            $"Your donation to {donation.Organization.Name} was confirmed.",
            "Donation",
            donation.Id,
            cancellationToken);
        await auditService.WriteAsync(
            "Confirm",
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
