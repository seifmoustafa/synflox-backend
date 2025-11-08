using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistory>
{
    public void Configure(EntityTypeBuilder<PasswordHistory> builder)
    {
        builder.HasOne(ph => ph.Admin)
            .WithMany()
            .HasForeignKey(ph => ph.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ph => ph.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(ph => ph.AdminId)
            .HasDatabaseName("IX_PasswordHistories_AdminId");

        builder.HasIndex(ph => ph.SetAt)
            .HasDatabaseName("IX_PasswordHistories_SetAt");
    }
}

