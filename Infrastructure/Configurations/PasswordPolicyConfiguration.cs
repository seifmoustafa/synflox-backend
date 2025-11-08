using Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PasswordPolicyConfiguration : IEntityTypeConfiguration<PasswordPolicy>
{
    public void Configure(EntityTypeBuilder<PasswordPolicy> builder)
    {
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_PasswordPolicies_IsActive")
            .HasFilter("[IsActive] = 1");

        // Ensure only one active policy at a time (enforced at service level)
    }
}

