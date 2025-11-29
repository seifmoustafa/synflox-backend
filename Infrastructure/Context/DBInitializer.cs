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

            // Seed Menu Items - Parent Structure with Dashboard First
            if (dbContext.MenuItems.Count() == 0)
            {
                // Parent Menu Items - Dashboard is Order 1 (first)
                var parentMenuItems = new List<MenuItems>
                {
                    // Dashboard Parent - Analytics & Insights (Order 1 - FIRST)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.dashboard",
                        Href = null,
                        Icon = "layout-dashboard",
                        Order = 1,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                        IsActive = true,
                    },
                    // System Parent - Admin Management & Core Data (Order 2)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.system",
                        Href = null,
                        Icon = "settings",
                        Order = 2,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    // Product Catalog Parent - Projects, Modules, Plans (Order 3)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.productCatalog",
                        Href = null,
                        Icon = "package",
                        Order = 3,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = null, // Visible to ALL user types
                        IsActive = true,
                    },
                    // License Management Parent - Subscriptions, License Keys (Order 4)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.licenseManagement",
                        Href = null,
                        Icon = "file-key",
                        Order = 4,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = null, // Visible to ALL user types
                        IsActive = true,
                    },
                };

                await dbContext.MenuItems.AddRangeAsync(parentMenuItems);
                await dbContext.SaveChangesAsync();

                // Get parent references
                var dashboardParent = parentMenuItems.First(m => m.Name == "nav.dashboard");
                var systemParent = parentMenuItems.First(m => m.Name == "nav.system");
                var productCatalogParent = parentMenuItems.First(m => m.Name == "nav.productCatalog");
                var licenseManagementParent = parentMenuItems.First(m => m.Name == "nav.licenseManagement");

                // Dashboard Children - Analytics & Insights Pages
                var dashboardChildren = new List<MenuItems>
                {
                    // new MenuItems
                    // {
                    //     Id = Guid.NewGuid(),
                    //     Name = "nav.overview",
                    //     Href = "/",
                    //     Icon = "home",
                    //     Order = 1,
                    //     ParentMenuItemsId = dashboardParent.Id,
                    //     AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    //     IsActive = true,
                    // },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.companiesDashboard",
                        Href = "/dashboard/companies",
                        Icon = "building-2",
                        Order = 2,
                        ParentMenuItemsId = dashboardParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.subscriptionsDashboard",
                        Href = "/dashboard/subscriptions",
                        Icon = "calendar-check",
                        Order = 3,
                        ParentMenuItemsId = dashboardParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.revenueDashboard",
                        Href = "/dashboard/revenue",
                        Icon = "dollar-sign",
                        Order = 4,
                        ParentMenuItemsId = dashboardParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }), // SuperAdmin only
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.activityDashboard",
                        Href = "/dashboard/activity",
                        Icon = "activity",
                        Order = 5,
                        ParentMenuItemsId = dashboardParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.alertsDashboard",
                        Href = "/dashboard/alerts",
                        Icon = "bell",
                        Order = 6,
                        ParentMenuItemsId = dashboardParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                        IsActive = true,
                    },
                };

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
                        AllowedUserTypes = null, // Visible to ALL user types
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
                        AllowedUserTypes = null, // Visible to ALL user types
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
                        AllowedUserTypes = null, // Visible to ALL user types
                        IsActive = true,
                    },
                };

                // License Management Children - Subscriptions & License Keys
                var licenseManagementChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.subscriptions",
                        Href = "/subscriptions",
                        Icon = "calendar-check",
                        Order = 1,
                        ParentMenuItemsId = licenseManagementParent.Id,
                        AllowedUserTypes = null, // Visible to ALL user types
                        IsActive = true,
                    },
                    // NOTE: License Keys will be added in Phase 4
                };

                // Add all child menu items
                await dbContext.MenuItems.AddRangeAsync(dashboardChildren);
                await dbContext.MenuItems.AddRangeAsync(systemChildren);
                await dbContext.MenuItems.AddRangeAsync(productCatalogChildren);
                await dbContext.MenuItems.AddRangeAsync(licenseManagementChildren);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
