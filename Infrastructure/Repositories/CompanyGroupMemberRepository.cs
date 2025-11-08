using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CompanyGroupMemberRepository : BaseRepository<Guid, CompanyGroupMember>, ICompanyGroupMemberRepository
{
    private readonly ApplicationDBContext _context;

    public CompanyGroupMemberRepository(ApplicationDBContext context) : base(context)
    {
        _context = context;
    }

    public async Task<bool> IsCompanyInGroupAsync(Guid companyId, Guid groupId)
    {
        return await _dbSet
            .AnyAsync(m => m.CompanyId == companyId 
                      && m.CompanyGroupId == groupId 
                      && !m.IsDeleted);
    }

    public async Task RemoveCompanyFromGroupAsync(Guid companyId, Guid groupId)
    {
        var member = await _dbSet
            .FirstOrDefaultAsync(m => m.CompanyId == companyId 
                                 && m.CompanyGroupId == groupId 
                                 && !m.IsDeleted);

        if (member != null)
        {
            member.IsDeleted = true;
            await UpdateAsync(member);
        }
    }

    public async Task RemoveAllCompaniesFromGroupAsync(Guid groupId)
    {
        var members = await _dbSet
            .Where(m => m.CompanyGroupId == groupId && !m.IsDeleted)
            .ToListAsync();

        foreach (var member in members)
        {
            member.IsDeleted = true;
            await UpdateAsync(member);
        }
    }
}



