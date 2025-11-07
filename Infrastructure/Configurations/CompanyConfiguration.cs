using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Licensing;

namespace Infrastructure.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            // Unique index for company name
            builder.HasIndex(c => c.Name)
                .IsUnique()
                .HasDatabaseName("IX_Companies_Name");

            // Index for license key lookups
            builder.HasIndex(c => c.LicenseKey)
                .HasDatabaseName("IX_Companies_LicenseKey")
                .HasFilter("[LicenseKey] IS NOT NULL");

            // Composite index for status queries (ExpiryDate, IsActive)
            builder.HasIndex(c => new { c.ExpiryDate, c.IsActive, c.IsDeleted })
                .HasDatabaseName("IX_Companies_Status");

            // Index for active companies
            builder.HasIndex(c => new { c.IsActive, c.IsDeleted })
                .HasDatabaseName("IX_Companies_Active_Deleted");

            // Configure string lengths
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.ContactEmail)
                .HasMaxLength(200);

            builder.Property(c => c.ContactPhone)
                .HasMaxLength(50);

            builder.Property(c => c.Address)
                .HasMaxLength(500);

            builder.Property(c => c.LicenseKey)
                .HasMaxLength(1000);
        }
    }
}

