using HairyPaws.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HairyPaws.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(notification => notification.ReferenceType)
            .HasMaxLength(100);

        builder.Property(notification => notification.CreatedAt)
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(notification => notification.ReadAt)
            .HasColumnType("timestamptz");

        builder.HasIndex(notification => notification.UserId);
        builder.HasIndex(notification => new { notification.UserId, notification.IsRead });
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt });

        builder.HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
