using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.CompanyId)
            .IsRequired();

        builder.Property(s => s.PlanId)
            .IsRequired();

        builder.Property(s => s.StartDateUtc)
            .IsRequired();

        builder.Property(s => s.ExpiryDateUtc)
            .IsRequired();

        builder.Property(s => s.StatusReason)
            .HasMaxLength(500);

        builder.Property(s => s.Amount)
            .HasColumnType("decimal(18,2)");

        // Relationships
        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.NextSubscription)
            .WithMany()
            .HasForeignKey(s => s.NextSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ParentSubscription)
            .WithMany()
            .HasForeignKey(s => s.ParentSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Access Control properties
        builder.Property(s => s.AccessMode)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.SubscriptionAccessMode.Full);

        builder.Property(s => s.EntitlementsVersion)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(s => s.AccessRestrictionMessage)
            .HasMaxLength(500);

        // Ignore computed properties
        builder.Ignore(s => s.IsLifetime);

        // Critical index for preventing overlapping active subscriptions
        builder.HasIndex(s => new { s.CompanyId, s.PlanId, s.IsActive, s.IsDeleted })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Subscriptions_CompanyPlan_Active");

        // Index for company subscriptions
        builder.HasIndex(s => new { s.CompanyId, s.IsActive, s.IsDeleted })
            .HasDatabaseName("IX_Subscriptions_Company_Active");

        // Index for expiry processing
        builder.HasIndex(s => new { s.ExpiryDateUtc, s.IsActive, s.IsExpired, s.IsDeleted })
            .HasDatabaseName("IX_Subscriptions_Expiry");

        // Index for auto-renewal processing
        builder.HasIndex(s => new { s.AutoRenew, s.IsActive, s.ExpiryDateUtc, s.IsDeleted })
            .HasDatabaseName("IX_Subscriptions_AutoRenew");

        // Index for deferred activation (scheduled next subscription)
        builder.HasIndex(s => new { s.NextSubscriptionId, s.NextSubscriptionActivationDateUtc, s.IsDeleted })
            .HasFilter("[NextSubscriptionId] IS NOT NULL")
            .HasDatabaseName("IX_Subscriptions_NextSubscription");

        // Index for access mode transitions (background job)
        builder.HasIndex(s => new { s.AccessMode, s.ExpiryDateUtc, s.IsActive, s.IsDeleted })
            .HasDatabaseName("IX_Subscriptions_AccessMode_Expiry");
    }
}
