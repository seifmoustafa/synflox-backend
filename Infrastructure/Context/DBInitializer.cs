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

            // Seed Menu Items - Simplified 2-Parent Structure
            if (dbContext.MenuItems.Count() == 0)
            {
                // Parent Menu Items (Only 2!)
                var parentMenuItems = new List<MenuItems>
                {
                    // System Parent - Admin Management & Reference Data (Lookups)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.System",
                        Href = null,
                        Icon = "settings",
                        Order = 1,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    // Phase 1 Parent - Subscription Engine
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Phase1",
                        Href = null,
                        Icon = "rocket",
                        Order = 2,
                        ParentMenuItemsId = null,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                await dbContext.MenuItems.AddRangeAsync(parentMenuItems);
                await dbContext.SaveChangesAsync();

                // Get parent references
                var systemParent = parentMenuItems.First(m => m.Name == "nav.System");
                var phase1Parent = parentMenuItems.First(m => m.Name == "nav.Phase1");

                // System Children - Admin Management & Lookups
                var systemChildren = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Admins",
                        Href = "/admins",
                        Icon = "user-shield",
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
                        Icon = "user-tag",
                        Order = 2,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Companies",
                        Href = "/companies",
                        Icon = "building",
                        Order = 3,
                        ParentMenuItemsId = systemParent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                // Phase 1 Children - Subscription Engine (Logically Arranged)
                var phase1Children = new List<MenuItems>
                {
                    // 1. Catalog Management (Building Blocks)
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Projects",
                        Href = "/projects",
                        Icon = "folder-kanban",
                        Order = 1,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.Modules",
                        Href = "/modules",
                        Icon = "puzzle",
                        Order = 2,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.SubscriptionPlans",
                        Href = "/plans",
                        Icon = "package",
                        Order = 3,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    
                    // 2. Subscription Operations
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.AllSubscriptions",
                        Href = "/subscriptions",
                        Icon = "calendar-check",
                        Order = 4,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.ActiveSubscriptions",
                        Href = "/subscriptions/active",
                        Icon = "badge-check",
                        Order = 5,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                    new MenuItems
                    {
                        Id = Guid.NewGuid(),
                        Name = "nav.CreateSubscription",
                        Href = "/subscriptions/create",
                        Icon = "plus-circle",
                        Order = 6,
                        ParentMenuItemsId = phase1Parent.Id,
                        AllowedUserTypes = JsonSerializer.Serialize(new List<string> { "SuperAdmin" }),
                        IsActive = true,
                    },
                };

                // Add all child menu items
                await dbContext.MenuItems.AddRangeAsync(systemChildren);
                await dbContext.MenuItems.AddRangeAsync(phase1Children);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
