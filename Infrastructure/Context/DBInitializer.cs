using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Authentication;
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

            var normalUserType = await dbContext.UserTypes
                .FirstOrDefaultAsync(u => u.AdminTypeName == "User");


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

            if (normalUserType == null)
            {
                normalUserType = new AdminType { AdminTypeName = "User", Id = Guid.NewGuid() };
                await dbContext.UserTypes.AddAsync(normalUserType);
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
                    AdminTypeId = superAdminUserType.Id
                };
                await dbContext.Admins.AddAsync(user);
                await dbContext.SaveChangesAsync();
            }
            //TODO: SEED Other tables
        }
    }
}
