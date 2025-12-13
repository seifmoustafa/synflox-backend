using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("OutboxEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        // Index for processing unprocessed events
        builder.HasIndex(e => new { e.IsProcessed, e.AttemptCount, e.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxEvents_Processing");

        // Index for subscription events
        builder.HasIndex(e => new { e.SubscriptionId, e.CreatedAtUtc })
            .HasDatabaseName("IX_OutboxEvents_Subscription");
    }
}
