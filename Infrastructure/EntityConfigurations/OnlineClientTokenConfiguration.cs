using Domain.Entities.OnlineAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class OnlineClientTokenConfiguration : IEntityTypeConfiguration<OnlineClientToken>
{
    public void Configure(EntityTypeBuilder<OnlineClientToken> builder)
    {
        builder.ToTable("OnlineClientTokens");

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

        builder.Property(t => t.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(t => t.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_OnlineClientTokens_TokenHash");

        builder.HasIndex(t => t.CompanyId)
            .HasDatabaseName("IX_OnlineClientTokens_CompanyId");

        builder.HasIndex(t => t.SubscriptionId)
            .HasDatabaseName("IX_OnlineClientTokens_SubscriptionId");

        builder.HasIndex(t => new { t.SubscriptionId, t.Status })
            .HasDatabaseName("IX_OnlineClientTokens_Subscription_Status");

        builder.HasIndex(t => t.ExpiresAtUtc)
            .HasDatabaseName("IX_OnlineClientTokens_ExpiresAt");

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Relationships
        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Subscription)
            .WithMany()
            .HasForeignKey(t => t.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.BoundDevices)
            .WithOne(d => d.Token)
            .HasForeignKey(d => d.TokenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
