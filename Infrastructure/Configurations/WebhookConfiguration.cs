using Domain.Entities.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        // Index for company lookups
        builder.HasIndex(w => new { w.CompanyId, w.IsActive, w.IsDeleted })
            .HasDatabaseName("IX_Webhooks_Company_Active_Deleted");

        // Foreign key to Company
        builder.HasOne(w => w.Company)
            .WithMany()
            .HasForeignKey(w => w.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure string lengths
        builder.Property(w => w.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(w => w.Secret)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.Events)
            .IsRequired()
            .HasMaxLength(500);
    }
}

