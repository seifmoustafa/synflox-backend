using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Services;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Entities.OnlineAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Context
{
    /// <summary>
    /// DbContext for writing to Master DB (SYNFLOX).
    /// Used by client-api for write operations that need to be visible to admin.
    /// Contains only the entities that client-api needs to write.
    /// </summary>
    public class MasterDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public MasterDbContext(DbContextOptions<MasterDbContext> options, ICurrentUserService? currentUserService = null)
            : base(options) 
        {
            _currentUserService = currentUserService;
        }

        #region Client Write Entities (synced to Master)
        
        /// <summary>
        /// License activations (device bindings) - written by client, read by admin
        /// </summary>
        public DbSet<LicenseActivation> LicenseActivations { get; set; }
        
        /// <summary>
        /// Device replacement requests - written by client, approved by admin
        /// </summary>
        public DbSet<DeviceReplacementRequest> DeviceReplacementRequests { get; set; }
        
        /// <summary>
        /// Online client tokens - managed by client admin
        /// </summary>
        public DbSet<OnlineClientToken> OnlineClientTokens { get; set; }
        
        /// <summary>
        /// Online device bindings - registered via online tokens
        /// </summary>
        public DbSet<OnlineDeviceBinding> OnlineDeviceBindings { get; set; }
        
        /// <summary>
        /// Company admin sessions - for audit trail
        /// </summary>
        public DbSet<CompanyAdminSession> CompanyAdminSessions { get; set; }
        
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations from the same assembly as ApplicationDBContext
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDbContext).Assembly);

            // SQL Server: Convert INT columns (0/1) to bool (true/false)
            var boolToIntConverter = new ValueConverter<bool, int>(
                v => v ? 1 : 0,
                v => v == 1 ? true : false);

            foreach (
                var entityType in modelBuilder
                    .Model.GetEntityTypes()
                    .Where(t => typeof(IBaseEntity).IsAssignableFrom(t.ClrType))
            )
            {
                var clrType = entityType.ClrType;
                var entityBuilder = modelBuilder.Entity(clrType);

                var isActiveProp = entityType.FindProperty("IsActive");
                if (isActiveProp != null)
                {
                    entityBuilder
                        .Property("IsActive")
                        .HasConversion(boolToIntConverter)
                        .HasColumnType("INT");
                }

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

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = base
                .ChangeTracker.Entries()
                .Where(e => e.Entity is AuditEntity<Guid> || e.Entity is AuditEntity<int>);

            Guid? currentUserId = null;
            try
            {
                currentUserId = _currentUserService?.UserId;
            }
            catch
            {
                // Silent - no user context in some operations
            }

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        ((dynamic)entry.Entity).CreatedTimestamp = DateTime.UtcNow;
                        if (currentUserId.HasValue)
                        {
                            ((dynamic)entry.Entity).CreatedBy = currentUserId.Value;
                        }
                        break;
                    case EntityState.Modified:
                        ((dynamic)entry.Entity).UpdatedTimestamp = DateTime.UtcNow;
                        if (currentUserId.HasValue)
                        {
                            ((dynamic)entry.Entity).UpdatedBy = currentUserId.Value;
                        }
                        break;
                    case EntityState.Deleted:
                        ((dynamic)entry.Entity).DeletedTimestamp = DateTime.UtcNow;
                        if (currentUserId.HasValue)
                        {
                            ((dynamic)entry.Entity).DeletedBy = currentUserId.Value;
                        }
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
