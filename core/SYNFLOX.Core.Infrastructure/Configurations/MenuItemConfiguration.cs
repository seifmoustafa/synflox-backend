using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities.Navigation;

namespace Infrastructure.Configurations
{
    public class MenuItemsConfiguration : IEntityTypeConfiguration<MenuItems>
    {
        public void Configure(EntityTypeBuilder<MenuItems> builder)
        {
            // Self-referencing relationship for parent-child hierarchy
            builder.HasOne(m => m.ParentMenuItems)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentMenuItemsId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Index for parent menu item lookups
            builder.HasIndex(m => m.ParentMenuItemsId)
                .HasDatabaseName("IX_MenuItemss_ParentMenuItemsId")
                .HasFilter("[ParentMenuItemsId] IS NOT NULL");

            // Index for ordering
            builder.HasIndex(m => new { m.Order, m.IsActive, m.IsDeleted })
                .HasDatabaseName("IX_MenuItemss_Order_Active_Deleted");

            // Index for active menu items
            builder.HasIndex(m => new { m.IsActive, m.IsDeleted })
                .HasDatabaseName("IX_MenuItemss_Active_Deleted");

            // Index for href lookups
            builder.HasIndex(m => m.Href)
                .HasDatabaseName("IX_MenuItemss_Href")
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

