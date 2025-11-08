using Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Index for name lookups
        builder.HasIndex(t => new { t.Name, t.IsDeleted })
            .HasDatabaseName("IX_Tenants_Name_Deleted");

        // Index for active tenants
        builder.HasIndex(t => new { t.IsActive, t.IsDeleted })
            .HasDatabaseName("IX_Tenants_Active_Deleted");

        // Configure string lengths
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.DatabaseConnectionString)
            .HasMaxLength(1000);
    }
}



