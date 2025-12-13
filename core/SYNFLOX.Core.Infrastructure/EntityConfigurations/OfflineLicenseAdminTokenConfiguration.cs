using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core configuration for OfflineLicenseAdminToken entity.
/// </summary>
public class OfflineLicenseAdminTokenConfiguration : IEntityTypeConfiguration<OfflineLicenseAdminToken>
{
    public void Configure(EntityTypeBuilder<OfflineLicenseAdminToken> builder)
    {
        builder.ToTable("OfflineLicenseAdminTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.RevocationReason)
            .HasMaxLength(500);

        builder.Property(t => t.LastUsedFromIp)
            .HasMaxLength(45);

        builder.Property(t => t.LastUsedUserAgent)
            .HasMaxLength(500);

        builder.Property(t => t.TokenVersion)
            .HasMaxLength(10)
            .HasDefaultValue("1.0");

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_OfflineLicenseAdminTokens_TokenHash");

        builder.HasIndex(t => t.CompanyId)
            .HasDatabaseName("IX_OfflineLicenseAdminTokens_CompanyId");

        builder.HasIndex(t => new { t.CompanyId, t.Status })
            .HasDatabaseName("IX_OfflineLicenseAdminTokens_CompanyId_Status");

        builder.HasIndex(t => t.ExpiresAtUtc)
            .HasDatabaseName("IX_OfflineLicenseAdminTokens_ExpiresAtUtc");

        // Relationships
        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
