using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Authentication;

namespace Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Unique indexes for username, email and phone number if provided
            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");
            
            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
            
            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("IX_Users_PhoneNumber");
            
            // Composite index for active user queries
            builder.HasIndex(u => new { u.IsActive, u.IsDeleted })
                .HasDatabaseName("IX_Users_Active_Deleted");
            
            // Index for verification status queries
            builder.HasIndex(u => new { u.IsEmailVerified, u.IsPhoneVerified })
                .HasDatabaseName("IX_Users_Verification");
            
            // Index for last login queries
            builder.HasIndex(u => u.LastLogin)
                .HasDatabaseName("IX_Users_LastLogin");
        }
    }
}
