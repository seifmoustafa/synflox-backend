using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for CompanyAdminSession entity.
/// Tracks individual login sessions for company admins.
/// </summary>
public class CompanyAdminSessionConfiguration : IEntityTypeConfiguration<CompanyAdminSession>
{
    public void Configure(EntityTypeBuilder<CompanyAdminSession> builder)
    {
        builder.ToTable("CompanyAdminSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.DeviceHash)
            .HasMaxLength(128);

        builder.Property(x => x.DeviceName)
            .HasMaxLength(200);

        builder.Property(x => x.OperatingSystem)
            .HasMaxLength(100);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);

        builder.Property(x => x.Location)
            .HasMaxLength(200);

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.ActionCount)
            .HasDefaultValue(0);

        builder.Property(x => x.EndNotes)
            .HasMaxLength(500);

        // Relationship with CompanyAdmin
        builder.HasOne(x => x.Admin)
            .WithMany(a => a.Sessions)
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.AdminId)
            .HasDatabaseName("IX_CompanyAdminSessions_AdminId");

        builder.HasIndex(x => x.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_CompanyAdminSessions_SessionId");

        builder.HasIndex(x => new { x.AdminId, x.IsActive })
            .HasDatabaseName("IX_CompanyAdminSessions_AdminId_IsActive");

        builder.HasIndex(x => x.StartedAtUtc)
            .HasDatabaseName("IX_CompanyAdminSessions_StartedAtUtc");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_CompanyAdminSessions_ExpiresAtUtc");

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
