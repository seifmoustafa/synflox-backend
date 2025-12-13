using Domain.Entities.Licensing;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for CompanyAdmin entity.
/// Defines company administrator accounts for device management.
/// </summary>
public class CompanyAdminConfiguration : IEntityTypeConfiguration<CompanyAdmin>
{
    public void Configure(EntityTypeBuilder<CompanyAdmin> builder)
    {
        builder.ToTable("CompanyAdmins");

        builder.HasKey(x => x.Id);

        // Credentials
        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Salt)
            .IsRequired()
            .HasMaxLength(100);

        // Profile
        builder.Property(x => x.DisplayName)
            .HasMaxLength(150);

        builder.Property(x => x.Email)
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .HasMaxLength(50);

        // Status
        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.MustChangePassword)
            .HasDefaultValue(true);

        builder.Property(x => x.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(x => x.MaxFailedAttempts)
            .HasDefaultValue(5);

        builder.Property(x => x.LockoutDurationMinutes)
            .HasDefaultValue(30);

        builder.Property(x => x.PasswordExpiryDays)
            .HasDefaultValue(0);

        // Session Configuration
        builder.Property(x => x.SessionPolicy)
            .HasDefaultValue(AdminSessionPolicy.SingleSession);

        builder.Property(x => x.SessionTimeoutMinutes)
            .HasDefaultValue(480);

        builder.Property(x => x.AutoLogoutOnInactivity)
            .HasDefaultValue(true);

        builder.Property(x => x.InactivityTimeoutMinutes)
            .HasDefaultValue(30);

        // Current Session
        builder.Property(x => x.CurrentSessionId)
            .HasMaxLength(100);

        builder.Property(x => x.CurrentDeviceHash)
            .HasMaxLength(128);

        builder.Property(x => x.CurrentDeviceName)
            .HasMaxLength(200);

        builder.Property(x => x.CurrentSessionIp)
            .HasMaxLength(45);

        // Audit
        builder.Property(x => x.LastLoginIp)
            .HasMaxLength(45);

        builder.Property(x => x.TotalLogins)
            .HasDefaultValue(0);

        // Permissions - defaults
        builder.Property(x => x.CanManageDevices).HasDefaultValue(true);
        builder.Property(x => x.CanViewSubscriptions).HasDefaultValue(true);
        builder.Property(x => x.CanApproveReplacements).HasDefaultValue(true);
        builder.Property(x => x.CanGenerateLicenses).HasDefaultValue(true);
        builder.Property(x => x.CanViewUsageReports).HasDefaultValue(true);
        builder.Property(x => x.CanModifySessionSettings).HasDefaultValue(true);

        // Relationship with Company (one-to-one)
        builder.HasOne(x => x.Company)
            .WithOne(c => c.Admin)
            .HasForeignKey<CompanyAdmin>(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.CompanyId)
            .IsUnique()
            .HasDatabaseName("IX_CompanyAdmins_CompanyId");

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("IX_CompanyAdmins_Username");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_CompanyAdmins_Email");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_CompanyAdmins_IsActive");

        builder.HasIndex(x => x.CurrentSessionId)
            .HasDatabaseName("IX_CompanyAdmins_CurrentSessionId");

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
