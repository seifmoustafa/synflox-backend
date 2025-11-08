using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanProjectModuleConfiguration : IEntityTypeConfiguration<PlanProjectModule>
{
    public void Configure(EntityTypeBuilder<PlanProjectModule> builder)
    {
        // Composite unique index to prevent duplicate plan-project-module relationships
        builder.HasIndex(ppm => new { ppm.SubscriptionPlanId, ppm.ProjectModuleId })
            .IsUnique()
            .HasDatabaseName("IX_PlanProjectModules_PlanId_ProjectModuleId");

        builder.HasOne(ppm => ppm.SubscriptionPlan)
            .WithMany(sp => sp.PlanProjectModules)
            .HasForeignKey(ppm => ppm.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ppm => ppm.ProjectModule)
            .WithMany(pm => pm.PlanProjectModules)
            .HasForeignKey(ppm => ppm.ProjectModuleId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete if project-module is deleted

        builder.HasIndex(ppm => ppm.SubscriptionPlanId)
            .HasDatabaseName("IX_PlanProjectModules_SubscriptionPlanId");

        builder.HasIndex(ppm => ppm.ProjectModuleId)
            .HasDatabaseName("IX_PlanProjectModules_ProjectModuleId");
    }
}

