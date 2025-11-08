using Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        // Unique index for report name (to prevent duplicates)
        builder.HasIndex(r => new { r.Name, r.IsDeleted })
            .IsUnique()
            .HasDatabaseName("IX_ReportDefinitions_Name_Deleted")
            .HasFilter("[IsDeleted] = 0");

        // Index for report type queries
        builder.HasIndex(r => new { r.ReportType, r.IsDeleted })
            .HasDatabaseName("IX_ReportDefinitions_Type_Deleted");

        // Index for active reports
        builder.HasIndex(r => new { r.IsActive, r.IsPreBuilt, r.IsDeleted })
            .HasDatabaseName("IX_ReportDefinitions_Active_PreBuilt_Deleted");

        // Configure string lengths
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.ReportType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Parameters)
            .HasMaxLength(2000);
    }
}

