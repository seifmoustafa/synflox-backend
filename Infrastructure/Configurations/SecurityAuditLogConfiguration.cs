using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
    {
        public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
        {
            builder.ToTable("SecurityAuditLogs");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.EventType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.EventDescription)
                .HasMaxLength(500);

            builder.Property(s => s.Username)
                .HasMaxLength(100);

            builder.Property(s => s.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6 max length

            builder.Property(s => s.UserAgent)
                .HasMaxLength(500);

            builder.Property(s => s.ErrorMessage)
                .HasMaxLength(500);

            builder.Property(s => s.Metadata)
                .HasMaxLength(2000);

            builder.Property(s => s.Success)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            // Indexes for performance
            builder.HasIndex(s => s.AdminId)
                .HasDatabaseName("IX_SecurityAuditLogs_AdminId");

            builder.HasIndex(s => s.EventType)
                .HasDatabaseName("IX_SecurityAuditLogs_EventType");

            builder.HasIndex(s => s.IpAddress)
                .HasDatabaseName("IX_SecurityAuditLogs_IpAddress");

            builder.HasIndex(s => s.CreatedAt)
                .HasDatabaseName("IX_SecurityAuditLogs_CreatedAt");

            builder.HasIndex(s => new { s.IpAddress, s.CreatedAt })
                .HasDatabaseName("IX_SecurityAuditLogs_IpAddress_CreatedAt");

            builder.HasIndex(s => new { s.EventType, s.Success })
                .HasDatabaseName("IX_SecurityAuditLogs_EventType_Success");

            // Foreign key relationship (nullable)
            builder.HasOne(s => s.Admin)
                .WithMany()
                .HasForeignKey(s => s.AdminId)
                .OnDelete(DeleteBehavior.SetNull); // Don't delete logs when admin deleted
        }
    }
}
