using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for SubscriptionEntitlement entity
/// </summary>
public class SubscriptionEntitlementConfiguration : IEntityTypeConfiguration<SubscriptionEntitlement>
{
    public void Configure(EntityTypeBuilder<SubscriptionEntitlement> builder)
    {
        builder.ToTable("SubscriptionEntitlements");

        builder.HasKey(e => e.Id);

        #region Core References

        builder.Property(e => e.SubscriptionId)
            .IsRequired();

        builder.Property(e => e.ProjectId);

        builder.Property(e => e.ModuleId);

        #endregion

        #region Grant Configuration

        builder.Property(e => e.GrantType)
            .IsRequired();

        builder.Property(e => e.AccessLevel)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.EntitlementAccessLevel.Full);

        builder.Property(e => e.Source)
            .IsRequired();

        builder.Property(e => e.IsCustom)
            .IsRequired()
            .HasDefaultValue(false);

        #endregion

        #region Operations

        builder.Property(e => e.CanCreate)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CanRead)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CanUpdate)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CanDelete)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CanExport)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.Features)
            .HasMaxLength(2000);

        builder.Property(e => e.UsageLimits)
            .HasMaxLength(2000);

        #endregion

        #region Display & Marketing

        builder.Property(e => e.Icon)
            .HasMaxLength(100);

        builder.Property(e => e.DisplayInMenu)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.UpgradeCta)
            .HasMaxLength(200);

        builder.Property(e => e.UpgradeUrl)
            .HasMaxLength(500);

        #endregion

        #region Audit & Expiry

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        #endregion

        #region Relationships

        builder.HasOne(e => e.Subscription)
            .WithMany(s => s.Entitlements)
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GrantedByAdmin)
            .WithMany()
            .HasForeignKey(e => e.GrantedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        #endregion

        #region Indexes

        // Primary index for subscription entitlements lookup
        builder.HasIndex(e => new { e.SubscriptionId, e.IsActive, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Active");

        // Index for project access check
        builder.HasIndex(e => new { e.SubscriptionId, e.ProjectId, e.IsActive, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0 AND [ProjectId] IS NOT NULL")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Project");

        // Index for module access check
        builder.HasIndex(e => new { e.SubscriptionId, e.ModuleId, e.IsActive, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0 AND [ModuleId] IS NOT NULL")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Module");

        // Index for full project/module lookup
        builder.HasIndex(e => new { e.SubscriptionId, e.ProjectId, e.ModuleId, e.IsActive, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Project_Module");

        // Index for source-based queries (e.g., find all admin grants)
        builder.HasIndex(e => new { e.SubscriptionId, e.Source, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Source");

        // Index for custom entitlements
        builder.HasIndex(e => new { e.SubscriptionId, e.IsCustom, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0 AND [IsCustom] = 1")
            .HasDatabaseName("IX_SubEntitlements_Subscription_Custom");

        // Index for expiring entitlements
        builder.HasIndex(e => new { e.ExpiresAt, e.IsActive, e.IsDeleted })
            .HasFilter("[IsDeleted] = 0 AND [ExpiresAt] IS NOT NULL")
            .HasDatabaseName("IX_SubEntitlements_Expiring");

        #endregion
    }
}
