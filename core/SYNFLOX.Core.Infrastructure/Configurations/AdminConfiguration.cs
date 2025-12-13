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
            
            // Unique index for email (prevent duplicate emails)
            builder.HasIndex(a => a.Email)
                .IsUnique()
                .HasDatabaseName("IX_Admins_Email")
                .HasFilter("[Email] IS NOT NULL"); // Allow null emails but enforce uniqueness on non-null
            
            // Index for admin type lookups
            builder.HasIndex(a => a.AdminTypeId)
                .HasDatabaseName("IX_Admins_AdminTypeId");
            
            // Composite index for active admin queries
            builder.HasIndex(a => new { a.IsActive, a.AdminTypeId })
                .HasDatabaseName("IX_Admins_Active_Type");
        }
    }
}

