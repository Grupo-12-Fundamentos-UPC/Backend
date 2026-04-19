using HairyPaws.Domain.Adoption.Entities;
using HairyPaws.Domain.Audit.Entities;
using HairyPaws.Application.Common.Interfaces;
using HairyPaws.Domain.Common.Abstractions;
using HairyPaws.Domain.Donations.Entities;
using HairyPaws.Domain.Events.Entities;
using HairyPaws.Domain.Identity.Entities;
using HairyPaws.Domain.Notifications.Entities;
using HairyPaws.Domain.Organizations.Entities;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Domain.Visits.Entities;
using Microsoft.EntityFrameworkCore;

namespace HairyPaws.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDateTimeProvider dateTimeProvider)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationDocument> OrganizationDocuments => Set<OrganizationDocument>();

    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<PetPhoto> PetPhotos => Set<PetPhoto>();

    public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();

    public DbSet<Visit> Visits => Set<Visit>();

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<DonationItem> DonationItems => Set<DonationItem>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(dateTimeProvider.UtcNow);
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities(DateTimeOffset utcNow)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                entry.Entity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
