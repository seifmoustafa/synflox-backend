using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Navigation;

namespace Infrastructure.Configurations
{
    public class AdminMenuItemConfiguration : IEntityTypeConfiguration<AdminMenuItem>
    {
        public void Configure(EntityTypeBuilder<AdminMenuItem> builder)
        {
            builder.ToTable("AdminMenuItems");

            // Self-referencing relationship for parent-child hierarchy
            builder.HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Index for parent menu item lookups
            builder.HasIndex(m => m.ParentId)
                .HasDatabaseName("IX_AdminMenuItems_ParentId")
                .HasFilter("[ParentId] IS NOT NULL");

            // Index for ordering
            builder.HasIndex(m => new { m.Order, m.IsActive, m.IsDeleted })
                .HasDatabaseName("IX_AdminMenuItems_Order_Active_Deleted");

            // Index for active menu items
            builder.HasIndex(m => new { m.IsActive, m.IsDeleted })
                .HasDatabaseName("IX_AdminMenuItems_Active_Deleted");

            // Index for href lookups
            builder.HasIndex(m => m.Href)
                .HasDatabaseName("IX_AdminMenuItems_Href")
                .HasFilter("[Href] IS NOT NULL");

            // Configure string lengths
            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(m => m.Href)
                .HasMaxLength(500);

            builder.Property(m => m.Icon)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.AllowedUserTypes)
                .HasMaxLength(500);
        }
    }
}
