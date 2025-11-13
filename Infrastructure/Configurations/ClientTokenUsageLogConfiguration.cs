using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ClientAccess;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for ClientTokenUsageLog
/// </summary>
public class ClientTokenUsageLogConfiguration : IEntityTypeConfiguration<ClientTokenUsageLog>
{
    public void Configure(EntityTypeBuilder<ClientTokenUsageLog> builder)
    {
        // Primary key
        builder.HasKey(log => log.Id);

        // Indexes for performance and analytics
        builder.HasIndex(log => new { log.ClientTokenId, log.RequestTimestampUtc })
            .HasDatabaseName("IX_ClientTokenUsageLogs_Token_Timestamp");

        builder.HasIndex(log => log.RequestTimestampUtc)
            .HasDatabaseName("IX_ClientTokenUsageLogs_Timestamp");

        builder.HasIndex(log => new { log.Endpoint, log.RequestTimestampUtc })
            .HasDatabaseName("IX_ClientTokenUsageLogs_Endpoint_Timestamp");

        builder.HasIndex(log => new { log.ClientIpAddress, log.RequestTimestampUtc })
            .HasDatabaseName("IX_ClientTokenUsageLogs_IP_Timestamp");

        builder.HasIndex(log => new { log.ResponseStatusCode, log.RequestTimestampUtc })
            .HasDatabaseName("IX_ClientTokenUsageLogs_Status_Timestamp");

        // String length constraints
        builder.Property(log => log.Endpoint)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(log => log.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(log => log.ClientIpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(log => log.UserAgent)
            .HasMaxLength(500);

        builder.Property(log => log.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(log => log.RequestMetadata)
            .HasMaxLength(2000);

        // Default values
        builder.Property(log => log.RequestTimestampUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        // Ignore computed property (calculated in C# code)
        builder.Ignore(log => log.IsSuccessful);

        // Relationships
        builder.HasOne(log => log.ClientToken)
            .WithMany(t => t.UsageLogs)
            .HasForeignKey(log => log.ClientTokenId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table configuration
        builder.ToTable("ClientTokenUsageLogs");

        // Partitioning hint for large datasets (implementation depends on database)
        // This could be implemented as a monthly partition strategy
    }
}
