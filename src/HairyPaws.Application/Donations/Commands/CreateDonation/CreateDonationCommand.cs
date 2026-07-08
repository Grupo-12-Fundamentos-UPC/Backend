using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Audit;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Application.Common.Mappings;
using HairyPaws.Application.Common.Security;
using HairyPaws.Application.Donations.Common;
using HairyPaws.Contracts.Donations.Requests;
using HairyPaws.Contracts.Donations.Responses;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Donations.Enums;
using HairyPaws.Domain.Notifications.Enums;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Donations.Commands.CreateDonation;

public sealed record CreateDonationCommand(
    Guid OrganizationId,
    string DonationType,
    decimal? Amount,
    string? TransactionId,
    string? Notes,
    IReadOnlyCollection<CreateDonationItemRequest> Items);

public sealed class CreateDonationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    INotificationService notificationService,
    IAuditService auditService)
    : ICommandHandler<CreateDonationCommand, DonationResponse>
{
    public async Task<DonationResponse> Handle(CreateDonationCommand command, CancellationToken cancellationToken)
    {
        var actor = await CurrentUserContext.GetRequiredCurrentUserAsync(dbContext, currentUserService, cancellationToken);
        var organization = await dbContext.Organizations
            .SingleOrDefaultAsync(entity => entity.Id == command.OrganizationId && entity.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The organization was not found.");

        var donationType = ContractEnumMapper.ToDonationType(command.DonationType);
        if (donationType == DonationType.Money && (!command.Amount.HasValue || command.Amount.Value <= 0))
        {
            throw new BusinessRuleViolationException("Money donations require an amount greater than zero.");
        }

        if (donationType == DonationType.Items && (command.Items is null || command.Items.Count == 0))
        {
            throw new BusinessRuleViolationException("Item donations require at least one donation item.");
        }

        var utcNow = dateTimeProvider.UtcNow;
        var donation = Donation.Create(
            actor.Id,
            organization.Id,
            donationType,
            donationType == DonationType.Money ? command.Amount : null,
            command.TransactionId,
            command.Notes,
            utcNow);

        if (donationType == DonationType.Items)
        {
            foreach (var item in command.Items)
            {
                donation.AddItem(item.Name, item.Quantity, item.Description, utcNow);
            }
        }

        await dbContext.Donations.AddAsync(donation, cancellationToken);
        await notificationService.CreateAsync(
            organization.OwnerUserId,
            NotificationType.DonationCreated,
            "New donation received",
            $"{actor.FirstName} {actor.LastName} created a {donationType} donation for your organization.",
            "Donation",
            donation.Id,
            cancellationToken);
        await auditService.WriteAsync(
            "Create",
            actor.Id,
            "Donation",
            donation.Id,
            before: null,
            after: donation.ToAuditSnapshot(),
            metadata: new
            {
                donation.OrganizationId,
                donation.DonorUserId
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
