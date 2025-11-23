using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Context
{
    /// <summary>
    /// Seeds the database with an initial SuperAdmin user. Adjust this logic to
    /// match your application's startup requirements.
    /// </summary>
    public class DBInitializer
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var dbContext = services.GetRequiredService<ApplicationDBContext>();
            await dbContext.Database.MigrateAsync();


            var superAdminUserType = await dbContext.UserTypes
                .FirstOrDefaultAsync(u => u.AdminTypeName == "SuperAdmin");

            var adminUserType = await dbContext.UserTypes
                .FirstOrDefaultAsync(u => u.AdminTypeName == "Admin");

            if (superAdminUserType == null)
            {
                superAdminUserType = new AdminType { AdminTypeName = "SuperAdmin", Id = Guid.NewGuid() };
                await dbContext.UserTypes.AddAsync(superAdminUserType);
                await dbContext.SaveChangesAsync();
            }

            if (adminUserType == null)
            {
                adminUserType = new AdminType { AdminTypeName = "Admin", Id = Guid.NewGuid() };
                await dbContext.UserTypes.AddAsync(adminUserType);
                await dbContext.SaveChangesAsync();
            }

            if (dbContext.Admins.Count() == 0)
            {
                var user = new Admin
                {
                    FirstName = "Super",
                    LastName = "Admin",
                    PhoneNumber = "0000000000",
                    Username = "superadmin",
                    Password = services.GetRequiredService<IPasswordHasher>().HashPassword("P@ssw0rd"),
                    Notes = "Super System Admin",
                    AdminTypeId = superAdminUserType.Id,
                };
                await dbContext.Admins.AddAsync(user);
                await dbContext.SaveChangesAsync();
            }

            // Seed Menu Items - 2-Parent Structure
            if (dbContext.MenuItems.Count() == 0)
            {
                // Parent Menu Items
                var parentMenuItems = new List<MenuItems>
                {
                    // System Parent - Admin Management & Core Data
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.system",
                        Href = null,
                        Icon = "settings",
                        Order = 1,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    // Product Catalog Parent - Projects, Modules, Plans
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.productCatalog",
                        Href = null,
                        Icon = "package",
                        Order = 2,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                await dbContext.MenuItems.AddRangeAsync(parentMenuItems);
                await dbContext.SaveChangesAsync();

                // Get parent references
                var systemParent = parentMenuItems.First(m => m.Name == "nav.system");
                var productCatalogParent = parentMenuItems.First(m => m.Name == "nav.productCatalog");

                // System Children - Admin Management & Core Data
                var systemChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.admins",
                        Href = "/admins",
                        Icon = "users",
                        Order = 1,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.adminTypes",
                        Href = "/admin-types",
                        Icon = "shield-check",
                        Order = 2,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.companies",
                        Href = "/companies",
                        Icon = "building-2",
                        Order = 3,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                // Product Catalog Children - Projects, Modules & Plans
                var productCatalogChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.projects",
                        Href = "/projects",
                        Icon = "folder-kanban",
                        Order = 1,
                        ParentMenuItemsId = productCatalogParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.modules",
                        Href = "/modules",
                        Icon = "boxes",
                        Order = 2,
                        ParentMenuItemsId = productCatalogParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.plans",
                        Href = "/plans",
                        Icon = "tag",
                        Order = 3,
                        ParentMenuItemsId = productCatalogParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    // NOTE: Subscriptions, and License Keys will be added as we implement them
                };

                // Add all child menu items
                await dbContext.MenuItems.AddRangeAsync(systemChildren);
                await dbContext.MenuItems.AddRangeAsync(productCatalogChildren);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
