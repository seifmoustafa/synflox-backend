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
                var MenuItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.System",
                        Href = null, // Parent item, no direct route
                        Icon = "settings",
                        Order = 1,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Subscribers",
                        Href = null, // Parent item, no direct route
                        Icon = "users",
                        Order = 2,
                        ParentMenuItemsId = null,
                        IsActive = true,
                    },
                };

                // Add child menu items for System
                var systemParent = MenuItems.First(m => m.Name == "nav.System");
                var systemChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Admins",
                        Href = "/admins",
                        Icon = "UserShield",
                        Order = 1,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.AdminTypes",
                        Href = "/admin-types",
                        Icon = "UserTag",
                        Order = 2,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                // Add child menu items for Subscribers
                var subscribersParent = MenuItems.First(m => m.Name == "nav.Subscribers");
                var subscribersChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Companies",
                        Href = "/companies",
                        Icon = "building",
                        Order = 1,
                        ParentMenuItemsId = subscribersParent.Id,
                        IsActive = true,
                    },
                };

                // Add all menu items to context
                await dbContext.MenuItems.AddRangeAsync(MenuItems);
                await dbContext.MenuItems.AddRangeAsync(systemChildren);
                await dbContext.MenuItems.AddRangeAsync(subscribersChildren);
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
                        IsActive = true
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        Name = "Status Summary Report",
                        Description = "Breakdown of companies by license status",
                        ReportType = "StatusSummary",
                        IsPreBuilt = true,
                        IsActive = true
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                        Name = "Usage Analytics Report",
                        Description = "API usage statistics by company",
                        ReportType = "UsageAnalytics",
                        IsPreBuilt = true,
                        Parameters = "{\"fromDate\": \"DateTime\", \"toDate\": \"DateTime\"}",
                        IsActive = true
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                        Name = "Trial Conversion Report",
                        Description = "Trial companies and conversion statistics",
                        ReportType = "TrialConversion",
                        IsPreBuilt = true,
                        IsActive = true
                    },
                    new ReportDefinition
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                        Name = "Module Usage Report",
                        Description = "Module usage statistics by company",
                        ReportType = "ModuleUsage",
                        IsPreBuilt = true,
                        IsActive = true
                    }
                };

                await dbContext.ReportDefinitions.AddRangeAsync(preBuiltReports);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
