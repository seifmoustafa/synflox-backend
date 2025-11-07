using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context
{
    /// <summary>
    /// Entity Framework context configured for SQL Server or Oracle.
    /// </summary>
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {

        }

        #region Admin
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminType> UserTypes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        #endregion

        #region Licensing
        public DbSet<Company> Companies { get; set; }
        #endregion

        #region References
        //public DbSet<EntityBaseModelClass> Entity { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply IEntityTypeConfiguration implementations from this assembly
            // so configurations like UserConfiguration are used automatically.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDBContext).Assembly);

            //Convert bool and Guid types when using Oracle
            if (Database.IsOracle())
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                    .Where(t => typeof(IBaseEntity).IsAssignableFrom(t.ClrType)))
                {
                    var clrType = entityType.ClrType;

                    modelBuilder.Entity(clrType)
                        .Property<bool>("IsActive")
                        .HasConversion<int>()
                        .HasColumnType("NUMBER(1)");

                    modelBuilder.Entity(clrType)
                        .Property<bool>("IsDeleted")
                        .HasConversion<int>()
                        .HasColumnType("NUMBER(1)");

                    var guidToStringConverter = new ValueConverter<Guid, string>(
                        v => v.ToString(),
                        v => Guid.Parse(v));

                    foreach (var property in entityType.GetProperties())
                    {
                        if (property.ClrType == typeof(Guid))
                        {
                            modelBuilder.Entity(clrType)
                                .Property(property.Name)
                                .HasConversion(guidToStringConverter)
                                .HasMaxLength(36)
                                .IsUnicode(false)
                                .HasColumnType("VARCHAR2(36)");
                        }
                    }

                }
            }

            // Additional model configuration can be added here if needed.


        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = base.ChangeTracker.Entries()
                .Where(e => e.Entity is AuditEntity<Guid> || e.Entity is AuditEntity<int>);

            foreach (var entry in entries)
            {

                switch (entry.State)
                {
                    case EntityState.Added:
                        ((dynamic)entry.Entity).CreatedTimestamp = DateTime.Now;
                        break;
                    case EntityState.Modified:
                        ((dynamic)entry.Entity).UpdatedTimestamp = DateTime.Now;
                        break;
                    case EntityState.Deleted:
                        ((dynamic)entry.Entity).DeletedTimestamp = DateTime.Now;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

    }
}
