using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CompanyGroupConfiguration : IEntityTypeConfiguration<CompanyGroup>
{
    public void Configure(EntityTypeBuilder<CompanyGroup> builder)
    {
        // Index for name lookups
        builder.HasIndex(g => new { g.Name, g.IsDeleted })
            .HasDatabaseName("IX_CompanyGroups_Name_Deleted");

        // Index for active groups
        builder.HasIndex(g => new { g.IsActive, g.IsDeleted })
            .HasDatabaseName("IX_CompanyGroups_Active_Deleted");

        // Configure string lengths
        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(1000);
    }
}



