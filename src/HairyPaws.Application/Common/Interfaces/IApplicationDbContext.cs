using HairyPaws.Domain.Audit.Entities;
using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Organizations.Entities;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AuditLog> AuditLogs { get; }

    DbSet<User> Users { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Organization> Organizations { get; }

    DbSet<OrganizationDocument> OrganizationDocuments { get; }

    DbSet<Pet> Pets { get; }

    DbSet<PetPhoto> PetPhotos { get; }

    DbSet<AdoptionRequest> AdoptionRequests { get; }

    DbSet<Visit> Visits { get; }

    DbSet<Donation> Donations { get; }

    DbSet<DonationItem> DonationItems { get; }

    DbSet<Event> Events { get; }

    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
