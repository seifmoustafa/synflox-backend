using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        // Unique index for plan name
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPlans_Name");

        // Index for active plans
        builder.HasIndex(p => new { p.IsActive, p.IsDeleted })
            .HasDatabaseName("IX_SubscriptionPlans_Active_Deleted");

        // Configure string lengths
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Features)
            .HasMaxLength(2000);
    }
}

