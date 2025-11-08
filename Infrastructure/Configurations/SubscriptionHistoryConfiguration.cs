using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SubscriptionHistoryConfiguration : IEntityTypeConfiguration<SubscriptionHistory>
{
    public void Configure(EntityTypeBuilder<SubscriptionHistory> builder)
    {
        // Index for company lookups
        builder.HasIndex(h => new { h.CompanyId, h.Timestamp })
            .HasDatabaseName("IX_SubscriptionHistory_Company_Timestamp");

        // Index for date range queries
        builder.HasIndex(h => new { h.Timestamp, h.IsDeleted })
            .HasDatabaseName("IX_SubscriptionHistory_Timestamp_Deleted");

        // Index for action type queries
        builder.HasIndex(h => new { h.ActionType, h.Timestamp })
            .HasDatabaseName("IX_SubscriptionHistory_ActionType_Timestamp");

        // Foreign key to Company
        builder.HasOne(h => h.Company)
            .WithMany()
            .HasForeignKey(h => h.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(h => h.OldValue)
            .HasMaxLength(2000);

        builder.Property(h => h.NewValue)
            .HasMaxLength(2000);

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        // Configure required fields
        builder.Property(h => h.CompanyId)
            .IsRequired();

        builder.Property(h => h.ActionType)
            .IsRequired();

        builder.Property(h => h.Timestamp)
            .IsRequired();
    }
}

