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
    /// Seeds the database with initial data for the admin portal.
    /// </summary>
    public class DBInitializer
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var dbContext = services.GetRequiredService<ApplicationDBContext>();
            await dbContext.Database.MigrateAsync();

            // Seed Admin Types
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

            // Seed Default Super Admin
            if (!dbContext.Admins.Any())
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

            // Seed Admin Portal Menu Items
            await SeedAdminMenuItemsAsync(dbContext);

            // Seed Client Portal Menu Items
            await SeedClientMenuItemsAsync(dbContext);
        }

        /// <summary>
        /// Seeds admin portal navigation menu items.
        /// </summary>
        private static async Task SeedAdminMenuItemsAsync(ApplicationDBContext dbContext)
        {
            if (dbContext.AdminMenuItems.Any()) return;

            // Parent Menu Items
            var parentMenuItems = new List<AdminMenuItem>
            {
                // Dashboard Parent - Analytics & Insights (Order 1)
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.dashboard",
                    Href = null,
                    Icon = "layout-dashboard",
                    Order = 1,
                    ParentId = null,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    IsActive = true,
                },
                // System Parent - Admin Management & Core Data (Order 2)
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.system",
                    Href = null,
                    Icon = "settings",
                    Order = 2,
                    ParentId = null,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
                // Product Catalog Parent - Projects, Modules, Plans (Order 3)
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.productCatalog",
                    Href = null,
                    Icon = "package",
                    Order = 3,
                    ParentId = null,
                    AllowedUserTypes = null, // Visible to ALL admin types
                    IsActive = true,
                },
                // License Management Parent - Subscriptions, License Keys (Order 4)
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.licenseManagement",
                    Href = null,
                    Icon = "file-key",
                    Order = 4,
                    ParentId = null,
                    AllowedUserTypes = null, // Visible to ALL admin types
                    IsActive = true,
                },
                // Communications Parent - Email & Notifications (Order 5)
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.communications",
                    Href = null,
                    Icon = "mail",
                    Order = 5,
                    ParentId = null,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
            };

            await dbContext.AdminMenuItems.AddRangeAsync(parentMenuItems);
            await dbContext.SaveChangesAsync();

            // Get parent references
            var dashboardParent = parentMenuItems.First(m => m.Name == "nav.dashboard");
            var systemParent = parentMenuItems.First(m => m.Name == "nav.system");
            var productCatalogParent = parentMenuItems.First(m => m.Name == "nav.productCatalog");
            var licenseManagementParent = parentMenuItems.First(m => m.Name == "nav.licenseManagement");
            var communicationsParent = parentMenuItems.First(m => m.Name == "nav.communications");

            // Dashboard Children
            var dashboardChildren = new List<AdminMenuItem>
            {
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.companiesDashboard",
                    Href = "/dashboard/companies",
                    Icon = "building-2",
                    Order = 1,
                    ParentId = dashboardParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.subscriptionsDashboard",
                    Href = "/dashboard/subscriptions",
                    Icon = "calendar-check",
                    Order = 2,
                    ParentId = dashboardParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.revenueDashboard",
                    Href = "/dashboard/revenue",
                    Icon = "dollar-sign",
                    Order = 3,
                    ParentId = dashboardParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.activityDashboard",
                    Href = "/dashboard/activity",
                    Icon = "activity",
                    Order = 4,
                    ParentId = dashboardParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.alertsDashboard",
                    Href = "/dashboard/alerts",
                    Icon = "bell",
                    Order = 5,
                    ParentId = dashboardParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin", "Admin" }),
                    IsActive = true,
                },
            };

            // System Children
            var systemChildren = new List<AdminMenuItem>
            {
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.admins",
                    Href = "/admins",
                    Icon = "users",
                    Order = 1,
                    ParentId = systemParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.adminTypes",
                    Href = "/admin-types",
                    Icon = "shield-check",
                    Order = 2,
                    ParentId = systemParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.companies",
                    Href = "/companies",
                    Icon = "building-2",
                    Order = 3,
                    ParentId = systemParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
            };

            // Product Catalog Children
            var productCatalogChildren = new List<AdminMenuItem>
            {
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.projects",
                    Href = "/projects",
                    Icon = "folder-kanban",
                    Order = 1,
                    ParentId = productCatalogParent.Id,
                    AllowedUserTypes = null,
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.modules",
                    Href = "/modules",
                    Icon = "boxes",
                    Order = 2,
                    ParentId = productCatalogParent.Id,
                    AllowedUserTypes = null,
                    IsActive = true,
                },
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.plans",
                    Href = "/plans",
                    Icon = "tag",
                    Order = 3,
                    ParentId = productCatalogParent.Id,
                    AllowedUserTypes = null,
                    IsActive = true,
                },
            };

            // License Management Children
            var licenseManagementChildren = new List<AdminMenuItem>
            {
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.subscriptions",
                    Href = "/subscriptions",
                    Icon = "calendar-check",
                    Order = 1,
                    ParentId = licenseManagementParent.Id,
                    AllowedUserTypes = null,
                    IsActive = true,
                },
            };

            // Communications Children - Including Custom Email
            var communicationsChildren = new List<AdminMenuItem>
            {
                new AdminMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.customEmail",
                    Href = "/communications/custom-email",
                    Icon = "send",
                    Order = 1,
                    ParentId = communicationsParent.Id,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                    IsActive = true,
                },
            };

            // Add all child menu items
            await dbContext.AdminMenuItems.AddRangeAsync(dashboardChildren);
            await dbContext.AdminMenuItems.AddRangeAsync(systemChildren);
            await dbContext.AdminMenuItems.AddRangeAsync(productCatalogChildren);
            await dbContext.AdminMenuItems.AddRangeAsync(licenseManagementChildren);
            await dbContext.AdminMenuItems.AddRangeAsync(communicationsChildren);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds client portal navigation menu items.
        /// </summary>
        private static async Task SeedClientMenuItemsAsync(ApplicationDBContext dbContext)
        {
            if (dbContext.ClientMenuItems.Any()) return;

            // Client Portal Menu Items - Nested Subscriptions Navigation
            var clientMenuItems = new List<ClientMenuItem>
            {
                // Dashboard - Home page
                new ClientMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.dashboard",
                    Href = "/",
                    Icon = "LayoutDashboard",
                    Order = 1,
                    ParentId = null,
                    RequiredPermissions = null, // Visible to all authenticated users
                    IsActive = true,
                },
                // My Subscriptions - Main subscriptions list and management
                new ClientMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.subscriptions",
                    Href = "/subscriptions",
                    Icon = "Package",
                    Order = 2,
                    ParentId = null,
                    RequiredPermissions = null, // Visible to all authenticated users
                    IsActive = true,
                },
                // Device Replacements - Replacement requests management
                new ClientMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.replacements",
                    Href = "/replacements",
                    Icon = "RefreshCw",
                    Order = 3,
                    ParentId = null,
                    RequiredPermissions = JsonSerializer.Serialize(new List<string> { "CanApproveReplacements" }),
                    IsActive = true,
                },
                // Settings - User preferences
                new ClientMenuItem
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.settings",
                    Href = "/settings",
                    Icon = "Settings",
                    Order = 4,
                    ParentId = null,
                    RequiredPermissions = null, // Visible to all authenticated users
                    IsActive = true,
                },
            };

            await dbContext.ClientMenuItems.AddRangeAsync(clientMenuItems);
            await dbContext.SaveChangesAsync();
        }
    }
}
