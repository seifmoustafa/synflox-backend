using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyGroupRepository : BaseRepository<Guid, CompanyGroup>, ICompanyGroupRepository
{
    private readonly ApplicationDBContext _context;

    public CompanyGroupRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Company>> GetCompaniesInGroupAsync(Guid groupId)
    {
        return await _context.Set<CompanyGroupMember>()
            .Where(m => m.CompanyGroupId == groupId && !m.IsDeleted)
            .Include(m => m.Company)
            .Where(m => !m.Company.IsDeleted)
            .Select(m => m.Company)
            .ToListAsync();
    }

    public async Task<IEnumerable<CompanyGroup>> GetGroupsForCompanyAsync(Guid companyId)
    {
        return await _context.Set<CompanyGroupMember>()
            .Where(m => m.CompanyId == companyId && !m.IsDeleted)
            .Include(m => m.CompanyGroup)
            .Where(m => !m.CompanyGroup.IsDeleted)
            .Select(m => m.CompanyGroup)
            .ToListAsync();
    }
}



