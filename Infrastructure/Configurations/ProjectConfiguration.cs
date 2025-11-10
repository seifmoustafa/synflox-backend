using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // Unique index for project name
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Projects_Name");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Features)
            .HasMaxLength(2000);

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Projects_IsActive");
    }
}

