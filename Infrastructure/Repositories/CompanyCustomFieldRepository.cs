using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyCustomFieldRepository : BaseRepository<Guid, CompanyCustomField>, ICompanyCustomFieldRepository
{
    public CompanyCustomFieldRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CompanyCustomField>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _dbSet
            .Where(f => f.CompanyId == companyId && !f.IsDeleted)
            .OrderBy(f => f.FieldName)
            .ToListAsync();
    }

    public async Task<CompanyCustomField?> GetByCompanyIdAndFieldNameAsync(Guid companyId, string fieldName)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.CompanyId == companyId 
                                 && f.FieldName == fieldName 
                                 && !f.IsDeleted);
    }
}



