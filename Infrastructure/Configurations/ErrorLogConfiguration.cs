using Domain.Entities.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        // Index for ErrorId lookups
        builder.HasIndex(e => new { e.ErrorId, e.IsDeleted })
            .HasDatabaseName("IX_ErrorLogs_ErrorId_Deleted")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Index for date range queries
        builder.HasIndex(e => new { e.Timestamp, e.IsDeleted })
            .HasDatabaseName("IX_ErrorLogs_Timestamp_Deleted");

        // Index for severity queries
        builder.HasIndex(e => new { e.Severity, e.Timestamp, e.IsDeleted })
            .HasDatabaseName("IX_ErrorLogs_Severity_Timestamp_Deleted");

        // Configure string lengths
        builder.Property(e => e.ErrorId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.ExceptionType)
            .HasMaxLength(500);

        builder.Property(e => e.HttpMethod)
            .HasMaxLength(10);

        builder.Property(e => e.RequestPath)
            .HasMaxLength(1000);

        builder.Property(e => e.QueryString)
            .HasMaxLength(2000);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45);

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasMaxLength(20);

        // Configure required fields
        builder.Property(e => e.Timestamp)
            .IsRequired();
    }
}



