using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for AccessTimeWindow entity.
/// Defines time windows for time-based concurrent access modes.
/// </summary>
public class AccessTimeWindowConfiguration : IEntityTypeConfiguration<AccessTimeWindow>
{
    public void Configure(EntityTypeBuilder<AccessTimeWindow> builder)
    {
        builder.ToTable("AccessTimeWindows");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100);

        builder.Property(x => x.TimezoneId)
            .HasMaxLength(100);

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // Relationship with Plan
        builder.HasOne(x => x.Plan)
            .WithMany(p => p.AccessTimeWindows)
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.PlanId)
            .HasDatabaseName("IX_AccessTimeWindows_PlanId");

        builder.HasIndex(x => new { x.PlanId, x.IsActive })
            .HasDatabaseName("IX_AccessTimeWindows_PlanId_IsActive");

        builder.HasIndex(x => new { x.PlanId, x.DayOfWeek })
            .HasDatabaseName("IX_AccessTimeWindows_PlanId_DayOfWeek");

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
