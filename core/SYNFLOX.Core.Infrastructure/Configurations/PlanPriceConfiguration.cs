using Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
{
    public void Configure(EntityTypeBuilder<PlanPrice> builder)
    {
        builder.ToTable("PlanPrices");

        builder.HasKey(pp => new { pp.PlanId, pp.Currency });

        builder.Property(pp => pp.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(pp => pp.Plan)
            .WithMany(p => p.PlanPrices)
            .HasForeignKey(pp => pp.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
