using Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        // Index for company lookups
        builder.HasIndex(n => new { n.CompanyId, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_Company_CreatedAt");

        // Index for unread notifications
        builder.HasIndex(n => new { n.CompanyId, n.IsRead, n.IsDeleted })
            .HasDatabaseName("IX_Notifications_Company_Read_Deleted");

        // Index for type queries
        builder.HasIndex(n => new { n.Type, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_Type_CreatedAt");

        // Foreign key to Company
        builder.HasOne(n => n.Company)
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        // Configure required fields
        builder.Property(n => n.CompanyId)
            .IsRequired();

        builder.Property(n => n.Type)
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .IsRequired();
    }
}

