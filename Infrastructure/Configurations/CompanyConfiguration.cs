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

            // Index for active companies (ExpiryDate and LicenseKey removed - now in Subscription entity)
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
        }
    }
}

