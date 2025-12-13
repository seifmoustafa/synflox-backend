using Domain.Entities.OnlineAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class SubscriptionChangeLogConfiguration : IEntityTypeConfiguration<SubscriptionChangeLog>
{
    public void Configure(EntityTypeBuilder<SubscriptionChangeLog> builder)
    {
        builder.ToTable("SubscriptionChangeLogs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ChangeType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ChangeDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.OldValue)
            .HasMaxLength(2000);

        builder.Property(c => c.NewValue)
            .HasMaxLength(2000);

        builder.Property(c => c.AppliedBy)
            .HasMaxLength(200);

        builder.Property(c => c.CancellationReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(c => c.SubscriptionId)
            .HasDatabaseName("IX_SubscriptionChangeLogs_SubscriptionId");

        builder.HasIndex(c => c.PlanId)
            .HasDatabaseName("IX_SubscriptionChangeLogs_PlanId");

        builder.HasIndex(c => new { c.SubscriptionId, c.IsApplied })
            .HasDatabaseName("IX_SubscriptionChangeLogs_Subscription_Applied");

        builder.HasIndex(c => c.EffectiveDateUtc)
            .HasDatabaseName("IX_SubscriptionChangeLogs_EffectiveDate");

        builder.HasIndex(c => new { c.IsApplied, c.IsCancelled, c.EffectiveDateUtc })
            .HasDatabaseName("IX_SubscriptionChangeLogs_ReadyToApply");

        // Soft delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);

        // Relationships
        builder.HasOne(c => c.Subscription)
            .WithMany()
            .HasForeignKey(c => c.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Plan)
            .WithMany()
            .HasForeignKey(c => c.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
