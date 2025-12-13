using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core configuration for DeviceReplacementRequest entity.
/// </summary>
public class DeviceReplacementRequestConfiguration : IEntityTypeConfiguration<DeviceReplacementRequest>
{
    public void Configure(EntityTypeBuilder<DeviceReplacementRequest> builder)
    {
        builder.ToTable("DeviceReplacementRequests");

        builder.HasKey(r => r.Id);

        // New device info
        builder.Property(r => r.NewMachineHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.NewDeviceName)
            .HasMaxLength(200);

        builder.Property(r => r.NewDeviceOs)
            .HasMaxLength(100);

        builder.Property(r => r.NewMacAddress)
            .HasMaxLength(50);

        builder.Property(r => r.NewMotherboardSerial)
            .HasMaxLength(200);

        // Old device info
        builder.Property(r => r.OldDeviceName)
            .HasMaxLength(200);

        builder.Property(r => r.OldMachineHash)
            .HasMaxLength(128);

        // Request details
        builder.Property(r => r.RequestedFromIp)
            .HasMaxLength(45);

        builder.Property(r => r.RequestedUserAgent)
            .HasMaxLength(500);

        builder.Property(r => r.RejectionReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(r => r.SubscriptionId)
            .HasDatabaseName("IX_DeviceReplacementRequests_SubscriptionId");

        builder.HasIndex(r => r.CompanyId)
            .HasDatabaseName("IX_DeviceReplacementRequests_CompanyId");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_DeviceReplacementRequests_Status");

        builder.HasIndex(r => new { r.CompanyId, r.Status })
            .HasDatabaseName("IX_DeviceReplacementRequests_CompanyId_Status");

        builder.HasIndex(r => r.ExpiresAtUtc)
            .HasDatabaseName("IX_DeviceReplacementRequests_ExpiresAtUtc");

        // Relationships - All NoAction to avoid cascade conflicts
        // SQL Server doesn't allow multiple cascade paths
        builder.HasOne(r => r.Subscription)
            .WithMany()
            .HasForeignKey(r => r.SubscriptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.OldActivation)
            .WithMany()
            .HasForeignKey(r => r.OldActivationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
