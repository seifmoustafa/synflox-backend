using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanProjectConfiguration : IEntityTypeConfiguration<PlanProject>
{
    public void Configure(EntityTypeBuilder<PlanProject> builder)
    {
        builder.ToTable("PlanProjects");

        builder.HasKey(pp => new { pp.PlanId, pp.ProjectId });

        builder.HasOne(pp => pp.Plan)
            .WithMany(p => p.PlanProjects)
            .HasForeignKey(pp => pp.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Project)
            .WithMany(p => p.PlanProjects)
            .HasForeignKey(pp => pp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
