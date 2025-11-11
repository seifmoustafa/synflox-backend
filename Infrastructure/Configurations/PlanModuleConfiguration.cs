using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanModuleConfiguration : IEntityTypeConfiguration<PlanModule>
{
    public void Configure(EntityTypeBuilder<PlanModule> builder)
    {
        builder.ToTable("PlanModules");

        builder.HasKey(pm => new { pm.PlanId, pm.ModuleId });

        builder.HasOne(pm => pm.Plan)
            .WithMany(p => p.PlanModules)
            .HasForeignKey(pm => pm.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Module)
            .WithMany(m => m.PlanModules)
            .HasForeignKey(pm => pm.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
