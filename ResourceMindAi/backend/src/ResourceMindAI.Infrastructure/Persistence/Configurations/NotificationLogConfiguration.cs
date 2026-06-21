using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResourceMindAI.Domain.Entities;

namespace ResourceMindAI.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.RecipientEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(notification => notification.NotificationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(notification => notification.SentAtUtc)
            .IsRequired();

        builder.Property(notification => notification.IsSuccess)
            .IsRequired();

        builder.Property(notification => notification.FailureReason)
            .HasMaxLength(500);

        builder.HasOne(notification => notification.Project)
            .WithMany()
            .HasForeignKey(notification => notification.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.RecipientUser)
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => new
        {
            notification.ProjectId,
            notification.RecipientUserId,
            notification.NotificationType,
            notification.SentAtUtc
        });
    }
}
