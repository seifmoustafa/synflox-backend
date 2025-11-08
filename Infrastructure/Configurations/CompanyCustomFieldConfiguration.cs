using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CompanyCustomFieldConfiguration : IEntityTypeConfiguration<CompanyCustomField>
{
    public void Configure(EntityTypeBuilder<CompanyCustomField> builder)
    {
        // Composite unique index for company-field name relationship
        builder.HasIndex(f => new { f.CompanyId, f.FieldName, f.IsDeleted })
            .HasDatabaseName("IX_CompanyCustomFields_Company_FieldName_Deleted")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Index for company lookups
        builder.HasIndex(f => new { f.CompanyId, f.IsDeleted })
            .HasDatabaseName("IX_CompanyCustomFields_Company_Deleted");

        // Foreign key to Company
        builder.HasOne(f => f.Company)
            .WithMany()
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure string lengths
        builder.Property(f => f.FieldName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.FieldType)
            .IsRequired()
            .HasMaxLength(50);
    }
}



