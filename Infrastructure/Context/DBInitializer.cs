using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Entities.Navigation;
using Domain.Entities.Reporting;
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
                    Password = services.GetRequiredService<IPasswordHasher>().HashPassword("password"),
                    Notes = "Super System Admin",
                    AdminTypeId = superAdminUserType.Id,
                };
                await dbContext.Admins.AddAsync(user);
                await dbContext.SaveChangesAsync();
            }

            // Seed Menu Items
            if (dbContext.MenuItems.Count() == 0)
            {
                // Step 1: Create all parent items (top-level and group headers)
                var dashboard = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.dashboard",
                    Href = "/",
                    Icon = "LayoutDashboard",
                    Order = 1,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var subscribers = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.subscribers",
                    Href = null, // Parent item, no direct route
                    Icon = "UsersRound",
                    Order = 2,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var system = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.system",
                    Href = null, // Parent item, no direct route
                    Icon = "Settings",
                    Order = 3,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var apiIntegration = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.api-integration",
                    Href = null, // Parent item, no direct route
                    Icon = "Code",
                    Order = 4,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var analyticsReports = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.analytics-reports",
                    Href = null, // Parent item, no direct route
                    Icon = "BarChart3",
                    Order = 5,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var systemManagement = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.system-management",
                    Href = null, // Parent item, no direct route
                    Icon = "Activity",
                    Order = 6,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var notifications = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.notifications",
                    Href = "/notifications",
                    Icon = "Bell",
                    Order = 7,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var settings = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.settings",
                    Href = "/settings",
                    Icon = "Settings",
                    Order = 8,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var profile = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.profile",
                    Href = "/profile",
                    Icon = "User",
                    Order = 9,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = null, // All authenticated users
                    IsActive = true,
                };

                var menuItems = new MenuItems
                {
                    Id = Guid.NewGuid(),
                    Name = "nav.menu-items",
                    Href = "/menu-items",
                    Icon = "Menu",
                    Order = 10,
                    ParentMenuItemsId = null,
                    AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }), // SuperAdmin only
                    IsActive = true,
                };

                // Add all parent items to context first
                var parentItems = new List<MenuItems>
                {
                    dashboard,
                    subscribers,
                    system,
                    apiIntegration,
                    analyticsReports,
                    systemManagement,
                    notifications,
                    settings,
                    profile,
                    menuItems,
                };

                await dbContext.MenuItems.AddRangeAsync(parentItems);
                await dbContext.SaveChangesAsync();

                // Step 2: Create child items for Subscribers
                var subscribersChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.companies",
                        Href = "/companies",
                        Icon = "Building2",
                        Order = 1,
                        ParentMenuItemsId = subscribers.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.company-groups",
                        Href = "/company-groups",
                        Icon = "UsersRound",
                        Order = 2,
                        ParentMenuItemsId = subscribers.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.subscription-plans",
                        Href = "/subscription-plans",
                        Icon = "CreditCard",
                        Order = 3,
                        ParentMenuItemsId = subscribers.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                };

                // Step 3: Create child items for System
                var systemChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.admins",
                        Href = "/admins",
                        Icon = "Users",
                        Order = 1,
                        ParentMenuItemsId = system.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.admin-types",
                        Href = "/admin-types",
                        Icon = "UserCog",
                        Order = 2,
                        ParentMenuItemsId = system.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.projects",
                        Href = "/projects",
                        Icon = "FolderKanban",
                        Order = 3,
                        ParentMenuItemsId = system.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.modules",
                        Href = "/modules",
                        Icon = "Package",
                        Order = 4,
                        ParentMenuItemsId = system.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                };

                // Step 4: Create child items for API Integration
                var apiIntegrationChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.api-keys",
                        Href = "/api-keys",
                        Icon = "Key",
                        Order = 1,
                        ParentMenuItemsId = apiIntegration.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.webhooks",
                        Href = "/webhooks",
                        Icon = "Webhook",
                        Order = 2,
                        ParentMenuItemsId = apiIntegration.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                };

                // Step 5: Create child items for Analytics & Reports
                var analyticsReportsChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.analytics",
                        Href = "/analytics",
                        Icon = "BarChart3",
                        Order = 1,
                        ParentMenuItemsId = analyticsReports.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.reports",
                        Href = "/reports",
                        Icon = "FileText",
                        Order = 2,
                        ParentMenuItemsId = analyticsReports.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                };

                // Step 6: Create child items for System Management
                var systemManagementChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.metrics",
                        Href = "/metrics",
                        Icon = "Activity",
                        Order = 1,
                        ParentMenuItemsId = systemManagement.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.error-logs",
                        Href = "/error-logs",
                        Icon = "AlertCircle",
                        Order = 2,
                        ParentMenuItemsId = systemManagement.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.login-attempts",
                        Href = "/login-attempts",
                        Icon = "Shield",
                        Order = 3,
                        ParentMenuItemsId = systemManagement.Id,
                        AllowedUserTypes = null, // All authenticated users
                        IsActive = true,
                    },
                };

                // Add all child items to context
                var allChildren = new List<MenuItems>();
                allChildren.AddRange(subscribersChildren);
                allChildren.AddRange(systemChildren);
                allChildren.AddRange(apiIntegrationChildren);
                allChildren.AddRange(analyticsReportsChildren);
                allChildren.AddRange(systemManagementChildren);

                await dbContext.MenuItems.AddRangeAsync(allChildren);
                await dbContext.SaveChangesAsync();
            }

            // Seed Pre-built Report Definitions
            if (dbContext.ReportDefinitions.Count() == 0)
            {
                var preBuiltReports = new List<ReportDefinition>
                {
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        Name = "Subscription Expiry Report",
                        Description = "Companies expiring within specified days",
                        ReportType = "SubscriptionExpiry",
                        IsPreBuilt = true,
                        Parameters = "{\"days\": 30}",
                        IsActive = true,
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Name = "Status Summary Report",
                        Description = "Breakdown of companies by license status",
                        ReportType = "StatusSummary",
                        IsPreBuilt = true,
                        IsActive = true,
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                        Name = "Usage Analytics Report",
                        Description = "API usage statistics by company",
                        ReportType = "UsageAnalytics",
                        IsPreBuilt = true,
                        Parameters = "{\"fromDate\": \"DateTime\", \"toDate\": \"DateTime\"}",
                        IsActive = true,
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                        Name = "Trial Conversion Report",
                        Description = "Trial companies and conversion statistics",
                        ReportType = "TrialConversion",
                        IsPreBuilt = true,
                        IsActive = true,
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                        Name = "Module Usage Report",
                        Description = "Module usage statistics by company",
                        ReportType = "ModuleUsage",
                        IsPreBuilt = true,
                        IsActive = true,
                    },
                };

                await dbContext.ReportDefinitions.AddRangeAsync(preBuiltReports);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
