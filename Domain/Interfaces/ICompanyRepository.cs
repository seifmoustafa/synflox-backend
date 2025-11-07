using System;
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

        /// <summary>
        /// Gets a company by its license key.
        /// </summary>
        Task<Company?> GetByLicenseKeyAsync(string licenseKey);
    }
}

