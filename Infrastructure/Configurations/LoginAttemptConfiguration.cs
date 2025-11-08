using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        // Index for username lookups
        builder.HasIndex(a => new { a.Username, a.AttemptedAt, a.IsDeleted })
            .HasDatabaseName("IX_LoginAttempts_Username_AttemptedAt_Deleted");

        // Index for IP address lookups
        builder.HasIndex(a => new { a.IpAddress, a.AttemptedAt, a.IsDeleted })
            .HasDatabaseName("IX_LoginAttempts_IpAddress_AttemptedAt_Deleted")
            .HasFilter("[IpAddress] IS NOT NULL");

        // Index for failed attempts
        builder.HasIndex(a => new { a.Success, a.AttemptedAt, a.IsDeleted })
            .HasDatabaseName("IX_LoginAttempts_Success_AttemptedAt_Deleted");

        // Configure string lengths
        builder.Property(a => a.Username)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);

        builder.Property(a => a.FailureReason)
            .HasMaxLength(500);

        // Configure required fields
        builder.Property(a => a.Success)
            .IsRequired();

        builder.Property(a => a.AttemptedAt)
            .IsRequired();
    }
}

