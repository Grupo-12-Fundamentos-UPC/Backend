using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Application.Common.Exceptions;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Identity.Enums;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Organizations.Entities;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Common.Security;

public static class CurrentUserContext
{
    public static Guid GetRequiredUserId(ICurrentUserService currentUserService)
    {
        return currentUserService.UserId ?? throw new UnauthorizedAppException("Authentication is required.");
    }

    public static async Task<User> GetRequiredCurrentUserAsync(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUserService);
        return await dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException("The current user was not found.");
    }

    public static Task<bool> CanManageOrganizationAsync(
        IApplicationDbContext dbContext,
        User actor,
        Organization organization,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(actor.Role == UserRole.Admin || (actor.Role == UserRole.Ong && organization.IsOwnedBy(actor.Id)));
    }

    public static async Task<bool> CanManagePetAsync(
        IApplicationDbContext dbContext,
        User actor,
        Pet pet,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin)
        {
            return true;
        }

        if (pet.IsOwnedByUser(actor.Id))
        {
            return true;
        }

        if (actor.Role != UserRole.Ong || pet.OrganizationId is null)
        {
            return false;
        }

        return await dbContext.Organizations.AnyAsync(
            organization => organization.Id == pet.OrganizationId && organization.OwnerUserId == actor.Id,
            cancellationToken);
    }

    public static async Task<Guid?> GetOwnedOrganizationIdAsync(
        IApplicationDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Organizations
            .Where(organization => organization.OwnerUserId == userId && organization.DeletedAt == null)
            .Select(organization => (Guid?)organization.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<bool> CanManageAdoptionRequestAsync(
        IApplicationDbContext dbContext,
        User actor,
        AdoptionRequest adoptionRequest,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin)
        {
            return true;
        }

        var pet = adoptionRequest.Pet;
        if (pet is null)
        {
            pet = await dbContext.Pets
                .SingleOrDefaultAsync(entity => entity.Id == adoptionRequest.PetId && entity.DeletedAt == null, cancellationToken);

            if (pet is null)
            {
                return false;
            }
        }

        return await CanManagePetAsync(dbContext, actor, pet, cancellationToken);
    }

    public static async Task<bool> CanAccessAdoptionRequestAsync(
        IApplicationDbContext dbContext,
        User actor,
        AdoptionRequest adoptionRequest,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin || adoptionRequest.IsOwnedByAdopter(actor.Id))
        {
            return true;
        }

        return await CanManageAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken);
    }

    public static async Task<bool> CanAccessVisitAsync(
        IApplicationDbContext dbContext,
        User actor,
        Visit visit,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin)
        {
            return true;
        }

        var adoptionRequest = visit.AdoptionRequest;
        if (adoptionRequest is null)
        {
            adoptionRequest = await dbContext.AdoptionRequests
                .SingleOrDefaultAsync(entity => entity.Id == visit.AdoptionRequestId, cancellationToken);

            if (adoptionRequest is null)
            {
                return false;
            }
        }

        return await CanAccessAdoptionRequestAsync(dbContext, actor, adoptionRequest, cancellationToken);
    }

    public static async Task<bool> CanManageDonationAsync(
        IApplicationDbContext dbContext,
        User actor,
        Donation donation,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin)
        {
            return true;
        }

        var organization = donation.Organization;
        if (organization is null)
        {
            organization = await dbContext.Organizations
                .SingleOrDefaultAsync(entity => entity.Id == donation.OrganizationId && entity.DeletedAt == null, cancellationToken);

            if (organization is null)
            {
                return false;
            }
        }

        return await CanManageOrganizationAsync(dbContext, actor, organization, cancellationToken);
    }

    public static async Task<bool> CanAccessDonationAsync(
        IApplicationDbContext dbContext,
        User actor,
        Donation donation,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin || donation.IsOwnedByDonor(actor.Id))
        {
            return true;
        }

        return await CanManageDonationAsync(dbContext, actor, donation, cancellationToken);
    }

    public static async Task<bool> CanManageEventAsync(
        IApplicationDbContext dbContext,
        User actor,
        Event eventEntity,
        CancellationToken cancellationToken)
    {
        if (actor.Role == UserRole.Admin)
        {
            return true;
        }

        var organization = eventEntity.Organization;
        if (organization is null)
        {
            organization = await dbContext.Organizations
                .SingleOrDefaultAsync(entity => entity.Id == eventEntity.OrganizationId && entity.DeletedAt == null, cancellationToken);

            if (organization is null)
            {
                return false;
            }
        }

        return await CanManageOrganizationAsync(dbContext, actor, organization, cancellationToken);
    }

    public static bool CanAccessNotification(User actor, Notification notification) => notification.IsOwnedBy(actor.Id);
}
