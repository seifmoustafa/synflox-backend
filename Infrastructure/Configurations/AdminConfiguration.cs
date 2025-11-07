using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class AdminConfiguration : IEntityTypeConfiguration<Admin>
    {
        public void Configure(EntityTypeBuilder<Admin> builder)
        {
            // Unique index for username
            builder.HasIndex(a => a.Username)
                .IsUnique()
                .HasDatabaseName("IX_Admins_Username");
            
            // Index for admin type lookups
            builder.HasIndex(a => a.AdminTypeId)
                .HasDatabaseName("IX_Admins_AdminTypeId");
            
            // Composite index for active admin queries
            builder.HasIndex(a => new { a.IsActive, a.AdminTypeId })
                .HasDatabaseName("IX_Admins_Active_Type");
        }
    }
}

