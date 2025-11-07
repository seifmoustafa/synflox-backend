using System;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CompanyRepository : BaseRepository<Guid, Company>, ICompanyRepository
    {
        public CompanyRepository(ApplicationDBContext context) : base(context)
        {
        }

        public async Task<Company?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
        }

        public async Task<Company?> GetByLicenseKeyAsync(string licenseKey)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.LicenseKey == licenseKey && !c.IsDeleted);
        }
    }
}

