using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Company entity operations.
    /// </summary>
    public interface ICompanyRepository : IBaseRepository<Guid, Company>
    {
        /// <summary>
        /// Gets a company by its name.
        /// </summary>
        Task<Company?> GetByNameAsync(string name);
        
        // Delete cascade support
        Task<int> GetSubscriptionsCountAsync(Guid companyId, CancellationToken cancellationToken = default);
        Task<List<string>> GetSubscriptionNamesAsync(Guid companyId, int take = 10, CancellationToken cancellationToken = default);
        Task<int> GetClientTokensCountAsync(Guid companyId, CancellationToken cancellationToken = default);
        Task<bool> HasRelatedRecordsAsync(Guid companyId, CancellationToken cancellationToken = default);
        Task SoftDeleteSubscriptionsAsync(Guid companyId, CancellationToken cancellationToken = default);
        Task SoftDeleteClientTokensAsync(Guid companyId, CancellationToken cancellationToken = default);
    }
}

