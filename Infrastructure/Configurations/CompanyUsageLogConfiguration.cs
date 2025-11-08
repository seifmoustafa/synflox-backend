using Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CompanyUsageLogConfiguration : IEntityTypeConfiguration<CompanyUsageLog>
{
    public void Configure(EntityTypeBuilder<CompanyUsageLog> builder)
    {
        // Index for company lookups
        builder.HasIndex(log => new { log.CompanyId, log.RequestTimestamp, log.IsDeleted })
            .HasDatabaseName("IX_CompanyUsageLogs_Company_Timestamp_Deleted");

        // Index for endpoint analytics
        builder.HasIndex(log => new { log.Endpoint, log.RequestTimestamp, log.IsDeleted })
            .HasDatabaseName("IX_CompanyUsageLogs_Endpoint_Timestamp_Deleted");

        // Index for date range queries
        builder.HasIndex(log => new { log.RequestTimestamp, log.IsDeleted })
            .HasDatabaseName("IX_CompanyUsageLogs_Timestamp_Deleted");

        // Foreign key to Company
        builder.HasOne(log => log.Company)
            .WithMany()
            .HasForeignKey(log => log.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(log => log.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(log => log.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(log => log.IpAddress)
            .HasMaxLength(45);

        builder.Property(log => log.UserAgent)
            .HasMaxLength(500);

        // Configure required fields
        builder.Property(log => log.RequestTimestamp)
            .IsRequired();
    }
}

