using Domain.Entities.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        // Index for webhook lookups
        builder.HasIndex(d => new { d.WebhookId, d.AttemptedAt, d.IsDeleted })
            .HasDatabaseName("IX_WebhookDeliveries_Webhook_AttemptedAt_Deleted");

        // Index for failed deliveries
        builder.HasIndex(d => new { d.Succeeded, d.AttemptNumber, d.IsDeleted })
            .HasDatabaseName("IX_WebhookDeliveries_Succeeded_Attempt_Deleted");

        // Foreign key to Webhook
        builder.HasOne(d => d.Webhook)
            .WithMany()
            .HasForeignKey(d => d.WebhookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(d => d.Payload)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(d => d.ResponseBody)
            .HasMaxLength(2000);

        builder.Property(d => d.ErrorMessage)
            .HasMaxLength(1000);

        // Configure required fields
        builder.Property(d => d.EventType)
            .IsRequired();

        builder.Property(d => d.AttemptedAt)
            .IsRequired();

        builder.Property(d => d.Succeeded)
            .IsRequired();
    }
}

