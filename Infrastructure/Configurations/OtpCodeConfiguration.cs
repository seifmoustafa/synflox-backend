using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
    {
        public void Configure(EntityTypeBuilder<OtpCode> builder)
        {
            // Indexes for frequently queried fields
            builder.HasIndex(otp => otp.UserId)
                .HasDatabaseName("IX_OtpCodes_UserId");
            
            builder.HasIndex(otp => new { otp.UserId, otp.Code, otp.Purpose })
                .HasDatabaseName("IX_OtpCodes_User_Code_Purpose");
            
            // Composite index for valid OTP lookups
            builder.HasIndex(otp => new { otp.UserId, otp.Purpose, otp.IsUsed, otp.ExpiresAt })
                .HasDatabaseName("IX_OtpCodes_User_Purpose_Used_Expires");
            
            // Index for cleanup queries
            builder.HasIndex(otp => otp.ExpiresAt)
                .HasDatabaseName("IX_OtpCodes_ExpiresAt");
        }
    }
}

