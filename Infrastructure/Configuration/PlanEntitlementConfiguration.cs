using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

/// <summary>
/// EF Core configuration for PlanEntitlement entity
/// </summary>
public class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.ToTable("PlanEntitlements");

        builder.HasKey(e => e.Id);

        // Plan relationship (required)
        builder.HasOne(e => e.Plan)
            .WithMany(p => p.Entitlements)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Project relationship (optional)
        builder.HasOne(e => e.Project)
            .WithMany()
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        // Module relationship (optional)
        builder.HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.PlanId);
        builder.HasIndex(e => new { e.PlanId, e.ProjectId, e.ModuleId })
            .IsUnique()
            .HasFilter("IsDeleted = 0"); // Unique constraint only for non-deleted records

        // Check constraint: must have Project OR Module (not both, not neither)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_PlanEntitlement_Target",
            "(ProjectId IS NOT NULL AND ModuleId IS NULL) OR (ProjectId IS NULL AND ModuleId IS NOT NULL)"
        ));

        // Property configurations
        builder.Property(e => e.Features)
            .HasMaxLength(500);

        builder.Property(e => e.AccessLevel)
            .HasConversion<int>();

        // Ignore computed properties
        builder.Ignore(e => e.TargetType);
        builder.Ignore(e => e.TargetName);
        builder.Ignore(e => e.HasFullAccess);
    }
}
