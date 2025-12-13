using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(p => p.ExpiresAt)
                .IsRequired();

            builder.Property(p => p.IpAddress)
                .HasMaxLength(45);  // IPv6 max length

            builder.Property(p => p.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(p => p.FailedAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            // Indexes for performance
            builder.HasIndex(p => p.AdminId)
                .HasDatabaseName("IX_PasswordResetTokens_AdminId");

            builder.HasIndex(p => p.ExpiresAt)
                .HasDatabaseName("IX_PasswordResetTokens_ExpiresAt");

            builder.HasIndex(p => new { p.AdminId, p.IsUsed, p.ExpiresAt })
                .HasDatabaseName("IX_PasswordResetTokens_AdminId_IsUsed_ExpiresAt");

            // Foreign key relationship
            builder.HasOne(p => p.Admin)
                .WithMany()
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
