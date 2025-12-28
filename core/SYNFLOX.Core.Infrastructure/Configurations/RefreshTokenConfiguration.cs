using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Indexes for Admin tokens
            builder.HasIndex(rt => rt.AdminId)
                .HasDatabaseName("IX_RefreshTokens_AdminId");
            
            // Index for CompanyAdmin tokens (Client Portal)
            builder.HasIndex(rt => rt.CompanyAdminId)
                .HasDatabaseName("IX_RefreshTokens_CompanyAdminId");
            
            builder.HasIndex(rt => rt.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");
            
            // Composite index for active Admin token lookups
            builder.HasIndex(rt => new { rt.AdminId, rt.IsActive, rt.Expires })
                .HasDatabaseName("IX_RefreshTokens_Admin_Active_Expires");
            
            // Composite index for active CompanyAdmin token lookups
            builder.HasIndex(rt => new { rt.CompanyAdminId, rt.IsActive, rt.Expires })
                .HasDatabaseName("IX_RefreshTokens_CompanyAdmin_Active_Expires");
            
            // Index for cleanup queries
            builder.HasIndex(rt => rt.Expires)
                .HasDatabaseName("IX_RefreshTokens_Expires");
            
            // Navigation relationships
            builder.HasOne(rt => rt.Admin)
                .WithMany()
                .HasForeignKey(rt => rt.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(rt => rt.CompanyAdmin)
                .WithMany()
                .HasForeignKey(rt => rt.CompanyAdminId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

