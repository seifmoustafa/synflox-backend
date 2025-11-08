using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context
{
    /// <summary>
    /// Entity Framework context configured for SQL Server or Oracle.
    /// </summary>
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options) { }

        #region Admin
        public DbSet<Admin> Admins { get; set; }
        public DbSet<AdminType> UserTypes { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Domain.Entities.Authentication.ApiKey> ApiKeys { get; set; }
        public DbSet<Domain.Entities.Authentication.LoginAttempt> LoginAttempts { get; set; }
        public DbSet<Domain.Entities.Authentication.PasswordHistory> PasswordHistories { get; set; }
        #endregion

        #region Settings
        public DbSet<Domain.Entities.Settings.PasswordPolicy> PasswordPolicies { get; set; }
        #endregion

        #region Licensing
        public DbSet<Company> Companies { get; set; }
        public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ProjectModule> ProjectModules { get; set; }
        public DbSet<PlanProjectModule> PlanProjectModules { get; set; }
        public DbSet<CompanyGroup> CompanyGroups { get; set; }
        public DbSet<CompanyGroupMember> CompanyGroupMembers { get; set; }
        public DbSet<CompanyCustomField> CompanyCustomFields { get; set; }
        #endregion

        #region Notifications
        public DbSet<Domain.Entities.Notifications.Notification> Notifications { get; set; }
        #endregion

        #region Webhooks
        public DbSet<Domain.Entities.Webhooks.Webhook> Webhooks { get; set; }
        public DbSet<Domain.Entities.Webhooks.WebhookDelivery> WebhookDeliveries { get; set; }
        #endregion

        #region Analytics
        public DbSet<Domain.Entities.Analytics.CompanyUsageLog> CompanyUsageLogs { get; set; }
        #endregion

        #region Metrics
        public DbSet<Domain.Entities.Metrics.SystemMetric> SystemMetrics { get; set; }
        #endregion

        #region Logging
        public DbSet<Domain.Entities.Logging.ErrorLog> ErrorLogs { get; set; }
        #endregion

        #region Tenancy
        public DbSet<Domain.Entities.Tenancy.Tenant> Tenants { get; set; }
        #endregion

        #region Navigation
        public DbSet<Domain.Entities.Navigation.MenuItems> MenuItems { get; set; }
        #endregion

        #region Reporting
        public DbSet<Domain.Entities.Reporting.ReportDefinition> ReportDefinitions { get; set; }
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

            //Convert bool and Guid types
            // IMPORTANT: This must run AFTER ApplyConfigurationsFromAssembly to override any existing configs
            // Using SQL Server - Oracle conversion is commented out below
            
            // ORACLE CONVERSION - COMMENTED OUT (Using SQL Server)
            // Uncomment this block only if you need to use Oracle database
            /*
            var isOracle = Database.IsOracle();

            if (isOracle)
            {
                // ORACLE: Explicit bool to int converter - Oracle doesn't have native boolean type
                var boolToIntConverter = new ValueConverter<bool, int>(
                    v => v ? 1 : 0, // Convert bool to int (true -> 1, false -> 0)
                    v => v == 1 ? true : false // Convert int to bool: 1 -> true, anything else -> false
                );

                foreach (
                    var entityType in modelBuilder
                        .Model.GetEntityTypes()
                        .Where(t => typeof(IBaseEntity).IsAssignableFrom(t.ClrType))
                )
                {
                    var clrType = entityType.ClrType;
                    var entityBuilder = modelBuilder.Entity(clrType);

                    // Reconfigure IsActive property with converter for Oracle
                    var isActiveProperty = entityType.FindProperty("IsActive");
                    if (isActiveProperty != null && isActiveProperty.ClrType == typeof(bool))
                    {
                        entityBuilder
                            .Property<bool>("IsActive")
                            .HasConversion(boolToIntConverter)
                            .HasColumnType("NUMBER(1)");
                    }

                    // Reconfigure IsDeleted property with converter for Oracle
                    var isDeletedProperty = entityType.FindProperty("IsDeleted");
                    if (isDeletedProperty != null && isDeletedProperty.ClrType == typeof(bool))
                    {
                        entityBuilder
                            .Property<bool>("IsDeleted")
                            .HasConversion(boolToIntConverter)
                            .HasColumnType("NUMBER(1)");
                    }

                    // GUID conversion - only apply for Oracle
                    var guidToStringConverter = new ValueConverter<Guid, string>(
                        v => v.ToString(),
                        v => Guid.Parse(v)
                    );

                    foreach (var property in entityType.GetProperties())
                    {
                        if (property.ClrType == typeof(Guid))
                        {
                            modelBuilder
                                .Entity(clrType)
                                .Property(property.Name)
                                .HasConversion(guidToStringConverter)
                                .HasMaxLength(36)
                                .IsUnicode(false)
                                .HasColumnType("VARCHAR2(36)");
                        }
                    }
                }
            }
            */

            // SQL Server: Convert INT columns (0/1) to bool (true/false)
            // This conversion handles INT type columns in SQL Server
            // CRITICAL: This must run AFTER all configurations to ensure it overrides everything
            var boolToIntConverter = new ValueConverter<bool, int>(
                v => v ? 1 : 0,                    // Entity to DB: true -> 1, false -> 0
                v => v == 1 ? true : false);       // DB to Entity: 1 -> true, 0 -> false (EXPLICIT)

            foreach (
                var entityType in modelBuilder
                    .Model.GetEntityTypes()
                    .Where(t => typeof(IBaseEntity).IsAssignableFrom(t.ClrType))
            )
            {
                var clrType = entityType.ClrType;
                var entityBuilder = modelBuilder.Entity(clrType);

                // CRITICAL: Force reconfigure IsActive to ensure conversion is applied
                // This overrides any previous configuration
                var isActiveProp = entityType.FindProperty("IsActive");
                if (isActiveProp != null)
                {
                    entityBuilder
                        .Property("IsActive")
                        .HasConversion(boolToIntConverter)
                        .HasColumnType("INT");
                }

                // CRITICAL: Force reconfigure IsDeleted to ensure conversion is applied
                // This overrides any previous configuration
                var isDeletedProp = entityType.FindProperty("IsDeleted");
                if (isDeletedProp != null)
                {
                    entityBuilder
                        .Property("IsDeleted")
                        .HasConversion(boolToIntConverter)
                        .HasColumnType("INT");
                }
            }
        }

        // Additional model configuration can be added here if needed.

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = base
                .ChangeTracker.Entries()
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
