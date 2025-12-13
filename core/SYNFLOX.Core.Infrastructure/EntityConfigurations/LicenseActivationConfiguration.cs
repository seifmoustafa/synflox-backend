using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core configuration for LicenseActivation entity
/// </summary>
public class LicenseActivationConfiguration : IEntityTypeConfiguration<LicenseActivation>
{
    public void Configure(EntityTypeBuilder<LicenseActivation> builder)
    {
        builder.ToTable("LicenseActivations");

        builder.HasKey(e => e.Id);

        // Subscription relationship
        builder.HasOne(e => e.Subscription)
            .WithMany()
            .HasForeignKey(e => e.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Company relationship (denormalized)
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for common queries
        builder.HasIndex(e => new { e.SubscriptionId, e.MachineHash })
            .HasDatabaseName("IX_LicenseActivations_Subscription_MachineHash");

        builder.HasIndex(e => new { e.SubscriptionId, e.IsActive })
            .HasDatabaseName("IX_LicenseActivations_Subscription_Active");

        builder.HasIndex(e => e.CompanyId)
            .HasDatabaseName("IX_LicenseActivations_Company");

        builder.HasIndex(e => e.LastSeenAtUtc)
            .HasDatabaseName("IX_LicenseActivations_LastSeen");

        // Filtered index for active activations only
        builder.HasIndex(e => new { e.SubscriptionId, e.LastSeenAtUtc })
            .HasDatabaseName("IX_LicenseActivations_Active_LastSeen")
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");

        // Column configurations
        builder.Property(e => e.MachineHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.DeviceName)
            .HasMaxLength(200);

        builder.Property(e => e.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(e => e.CpuId)
            .HasMaxLength(100);

        builder.Property(e => e.MotherboardSerial)
            .HasMaxLength(100);

        builder.Property(e => e.DiskSerial)
            .HasMaxLength(100);

        builder.Property(e => e.MacAddress)
            .HasMaxLength(50);

        builder.Property(e => e.LastIpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.DeactivationReason)
            .HasMaxLength(500);

        builder.Property(e => e.LastUserAgent)
            .HasMaxLength(500);
    }
}
