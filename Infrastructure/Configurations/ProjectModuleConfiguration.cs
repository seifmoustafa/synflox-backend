using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ProjectModuleConfiguration : IEntityTypeConfiguration<ProjectModule>
{
    public void Configure(EntityTypeBuilder<ProjectModule> builder)
    {
        // Composite unique index to prevent duplicate project-module relationships
        builder.HasIndex(pm => new { pm.ProjectId, pm.ModuleId })
            .IsUnique()
            .HasDatabaseName("IX_ProjectModules_ProjectId_ModuleId");

        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectModules)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Module)
            .WithMany(m => m.ProjectModules)
            .HasForeignKey(pm => pm.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pm => pm.ProjectId)
            .HasDatabaseName("IX_ProjectModules_ProjectId");

        builder.HasIndex(pm => pm.ModuleId)
            .HasDatabaseName("IX_ProjectModules_ModuleId");
    }
}

