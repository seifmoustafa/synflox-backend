using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        // Unique index for key hash lookups
        builder.HasIndex(k => k.KeyHash)
            .IsUnique()
            .HasDatabaseName("IX_ApiKeys_KeyHash")
            .HasFilter("[KeyHash] IS NOT NULL AND [IsDeleted] = 0");

        // Index for company lookups
        builder.HasIndex(k => new { k.CompanyId, k.IsDeleted })
            .HasDatabaseName("IX_ApiKeys_Company_Deleted");

        // Index for active keys
        builder.HasIndex(k => new { k.IsActive, k.IsDeleted, k.ExpiresAt })
            .HasDatabaseName("IX_ApiKeys_Active_Deleted_Expires");

        // Foreign key to Company
        builder.HasOne(k => k.Company)
            .WithMany()
            .HasForeignKey(k => k.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(k => k.KeyHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(k => k.KeyPrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(k => k.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.AllowedIps)
            .HasMaxLength(2000);
    }
}

