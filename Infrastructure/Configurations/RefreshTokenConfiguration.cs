using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            // Indexes for frequently queried fields
            builder.HasIndex(rt => rt.AdminId)
                .HasDatabaseName("IX_RefreshTokens_AdminId");
            
            builder.HasIndex(rt => rt.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");
            
            // Composite index for active token lookups
            builder.HasIndex(rt => new { rt.AdminId, rt.IsActive, rt.Expires })
                .HasDatabaseName("IX_RefreshTokens_Admin_Active_Expires");
            
            // Index for cleanup queries
            builder.HasIndex(rt => rt.Expires)
                .HasDatabaseName("IX_RefreshTokens_Expires");
        }
    }
}
