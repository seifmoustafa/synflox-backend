using Domain.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations
{
    public class BackupCodeConfiguration : IEntityTypeConfiguration<BackupCode>
    {
        public void Configure(EntityTypeBuilder<BackupCode> builder)
        {
            builder.ToTable("BackupCodes");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.CodeHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(b => b.IsUsed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(b => b.CreatedAt)
                .IsRequired();

            builder.Property(b => b.BatchId)
                .IsRequired();

            // Indexes for performance
            builder.HasIndex(b => b.AdminId)
                .HasDatabaseName("IX_BackupCodes_AdminId");

            builder.HasIndex(b => b.IsUsed)
                .HasDatabaseName("IX_BackupCodes_IsUsed");

            builder.HasIndex(b => new { b.AdminId, b.IsUsed })
                .HasDatabaseName("IX_BackupCodes_AdminId_IsUsed");

            builder.HasIndex(b => new { b.AdminId, b.CodeHash })
                .HasDatabaseName("IX_BackupCodes_AdminId_CodeHash");

            builder.HasIndex(b => b.BatchId)
                .HasDatabaseName("IX_BackupCodes_BatchId");

            // Foreign key relationship
            builder.HasOne(b => b.Admin)
                .WithMany()
                .HasForeignKey(b => b.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
