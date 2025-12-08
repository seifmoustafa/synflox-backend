using Domain.Entities.OnlineAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class OnlineDeviceBindingConfiguration : IEntityTypeConfiguration<OnlineDeviceBinding>
{
    public void Configure(EntityTypeBuilder<OnlineDeviceBinding> builder)
    {
        builder.ToTable("OnlineDeviceBindings");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceFingerprint)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.DeviceName)
            .HasMaxLength(100);

        builder.Property(d => d.DeviceType)
            .HasMaxLength(50);

        builder.Property(d => d.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(d => d.LastIpAddress)
            .HasMaxLength(45);

        builder.Property(d => d.LastUserAgent)
            .HasMaxLength(500);

        builder.Property(d => d.StatusReason)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(d => new { d.SubscriptionId, d.DeviceFingerprint })
            .IsUnique()
            .HasDatabaseName("IX_OnlineDeviceBindings_Subscription_Fingerprint");

        builder.HasIndex(d => d.TokenId)
            .HasDatabaseName("IX_OnlineDeviceBindings_TokenId");

        builder.HasIndex(d => d.SubscriptionId)
            .HasDatabaseName("IX_OnlineDeviceBindings_SubscriptionId");

        builder.HasIndex(d => new { d.SubscriptionId, d.Status })
            .HasDatabaseName("IX_OnlineDeviceBindings_Subscription_Status");

        // Soft delete filter
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Relationships
        builder.HasOne(d => d.Token)
            .WithMany(t => t.BoundDevices)
            .HasForeignKey(d => d.TokenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Subscription)
            .WithMany()
            .HasForeignKey(d => d.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
