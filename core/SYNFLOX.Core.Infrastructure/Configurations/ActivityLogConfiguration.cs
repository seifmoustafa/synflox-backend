using Domain.Entities.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder.ToTable("ActivityLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.ActionType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.EntityId)
                .IsRequired();

            builder.Property(a => a.EntityName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.Description)
                .HasMaxLength(500);

            builder.Property(a => a.PerformedByName)
                .HasMaxLength(100);

            builder.Property(a => a.Timestamp)
                .IsRequired();

            builder.Property(a => a.Metadata)
                .HasMaxLength(4000);

            builder.Property(a => a.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            // Indexes for fast querying
            builder.HasIndex(a => a.Timestamp)
                .HasDatabaseName("IX_ActivityLogs_Timestamp")
                .IsDescending();

            builder.HasIndex(a => a.EntityType)
                .HasDatabaseName("IX_ActivityLogs_EntityType");

            builder.HasIndex(a => a.EntityId)
                .HasDatabaseName("IX_ActivityLogs_EntityId");

            builder.HasIndex(a => a.PerformedBy)
                .HasDatabaseName("IX_ActivityLogs_PerformedBy");

            builder.HasIndex(a => a.ActionType)
                .HasDatabaseName("IX_ActivityLogs_ActionType");

            // Composite index for recent activities by entity type
            builder.HasIndex(a => new { a.EntityType, a.Timestamp })
                .HasDatabaseName("IX_ActivityLogs_EntityType_Timestamp");
        }
    }
}
