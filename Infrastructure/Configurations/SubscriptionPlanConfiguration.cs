using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.DurationType)
            .IsRequired()
            .HasConversion<int>();  // Store enum as int

        builder.Property(p => p.CustomFeatures)
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

        // Unique index for name
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_SubscriptionPlans_Name");

        // Index for trial-enabled plans
        builder.HasIndex(p => new { p.AllowTrial, p.IsDeleted })
            .HasDatabaseName("IX_SubscriptionPlans_Trial");

        #region Free Tier & Fallback Configuration

        builder.Property(p => p.IsFreeTier)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.FallbackAccessMode)
            .IsRequired()
            .HasDefaultValue(Domain.Enums.SubscriptionAccessMode.ReadOnly);

        builder.Property(p => p.ExportGraceDays)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(p => p.ShowLockedModulesInMenu)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.LockedItemStyle)
            .HasMaxLength(50)
            .HasDefaultValue("greyed_with_lock");

        // Self-referencing relationship for default fallback plan
        builder.HasOne(p => p.DefaultFallbackPlan)
            .WithMany()
            .HasForeignKey(p => p.DefaultFallbackPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed property
        builder.Ignore(p => p.IsLifetimePlan);

        // Index for free tier plans
        builder.HasIndex(p => new { p.IsFreeTier, p.IsDeleted })
            .HasFilter("[IsFreeTier] = 1 AND [IsDeleted] = 0")
            .HasDatabaseName("IX_SubscriptionPlans_FreeTier");

        #endregion
    }
}
