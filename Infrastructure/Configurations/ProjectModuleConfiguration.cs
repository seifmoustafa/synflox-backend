using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ProjectModuleConfiguration : IEntityTypeConfiguration<ProjectModule>
{
    public void Configure(EntityTypeBuilder<ProjectModule> builder)
    {
        builder.ToTable("ProjectModules");

        builder.HasKey(pm => new { pm.ProjectId, pm.ModuleId });

        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectModules)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Module)
            .WithMany(m => m.ProjectModules)
            .HasForeignKey(pm => pm.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
