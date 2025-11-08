using System;
using System.Threading.Tasks;
using Domain.Entities.Tenancy;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for Tenant entities.
/// </summary>
public interface ITenantRepository : IBaseRepository<Guid, Tenant>
{
    /// <summary>
    /// Gets tenant by name.
    /// </summary>
    Task<Tenant?> GetByNameAsync(string name);
}



