using Domain.Entities.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SystemMetricConfiguration : IEntityTypeConfiguration<SystemMetric>
{
    public void Configure(EntityTypeBuilder<SystemMetric> builder)
    {
        // Index for metric type and timestamp queries
        builder.HasIndex(m => new { m.MetricType, m.Timestamp, m.IsDeleted })
            .HasDatabaseName("IX_SystemMetrics_Type_Timestamp_Deleted");

        // Index for aggregated metrics
        builder.HasIndex(m => new { m.MetricType, m.AggregationPeriod, m.Timestamp, m.IsDeleted })
            .HasDatabaseName("IX_SystemMetrics_Type_Aggregation_Timestamp_Deleted")
            .HasFilter("[AggregationPeriod] IS NOT NULL");

        // Index for cleanup queries
        builder.HasIndex(m => new { m.Timestamp, m.IsDeleted })
            .HasDatabaseName("IX_SystemMetrics_Timestamp_Deleted");

        // Configure string lengths
        builder.Property(m => m.Tags)
            .HasMaxLength(2000);

        builder.Property(m => m.AggregationPeriod)
            .HasMaxLength(50);

        // Configure required fields
        builder.Property(m => m.MetricType)
            .IsRequired();

        builder.Property(m => m.Value)
            .IsRequired()
            .HasColumnType("FLOAT");

        builder.Property(m => m.Timestamp)
            .IsRequired();
    }
}



