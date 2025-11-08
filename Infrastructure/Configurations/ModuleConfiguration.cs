using Domain.Entities.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        // Unique index for module name
        builder.HasIndex(m => m.Name)
            .IsUnique()
            .HasDatabaseName("IX_Modules_Name");

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("IX_Modules_IsActive");
    }
}

