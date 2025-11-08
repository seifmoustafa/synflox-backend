using System;
using System.Threading.Tasks;
using Domain.Entities.Authentication;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for ApiKey entity operations.
/// </summary>
public interface IApiKeyRepository : IBaseRepository<Guid, ApiKey>
{
    /// <summary>
    /// Gets an API key by its hash.
    /// </summary>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash);

    /// <summary>
    /// Gets all API keys for a company.
    /// </summary>
    Task<System.Collections.Generic.IEnumerable<ApiKey>> GetByCompanyIdAsync(Guid companyId, int skip = 0, int take = 10);

    /// <summary>
    /// Counts API keys for a company.
    /// </summary>
    Task<int> CountByCompanyIdAsync(Guid companyId);
}

