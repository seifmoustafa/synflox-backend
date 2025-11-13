using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.ClientAccess;
using Domain.Enums;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for ClientAccessToken
/// </summary>
public class ClientAccessTokenConfiguration : IEntityTypeConfiguration<ClientAccessToken>
{
    public void Configure(EntityTypeBuilder<ClientAccessToken> builder)
    {
        // Primary key
        builder.HasKey(t => t.Id);

        // Indexes for performance
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_ClientAccessTokens_TokenHash");

        builder.HasIndex(t => new { t.CompanyId, t.Status, t.IsDeleted })
            .HasDatabaseName("IX_ClientAccessTokens_Company_Status_Deleted");

        builder.HasIndex(t => new { t.SubscriptionId, t.Status, t.IsDeleted })
            .HasDatabaseName("IX_ClientAccessTokens_Subscription_Status_Deleted");

        builder.HasIndex(t => new { t.ExpiresAtUtc, t.Status })
            .HasDatabaseName("IX_ClientAccessTokens_Expiry_Status");

        builder.HasIndex(t => t.LastUsedAtUtc)
            .HasDatabaseName("IX_ClientAccessTokens_LastUsed");

        // String length constraints
        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex string

        builder.Property(t => t.RevocationReason)
            .HasMaxLength(500);

        builder.Property(t => t.LastUsedFromIp)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(t => t.LastUsedUserAgent)
            .HasMaxLength(500);

        builder.Property(t => t.AllowedEndpoints)
            .HasMaxLength(2000);

        builder.Property(t => t.TokenVersion)
            .HasMaxLength(10)
            .HasDefaultValue("1.0");

        // Enum conversion
        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(ClientTokenStatus.Active);

        // Default values
        builder.Property(t => t.UsageCount)
            .HasDefaultValue(0);

        builder.Property(t => t.IssuedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Subscription)
            .WithMany()
            .HasForeignKey(t => t.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.UsageLogs)
            .WithOne(log => log.ClientToken)
            .HasForeignKey(log => log.ClientTokenId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties (they are calculated in C# code)
        builder.Ignore(t => t.IsValid);
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.DaysUntilExpiry);

        // Table configuration
        builder.ToTable("ClientAccessTokens");

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
