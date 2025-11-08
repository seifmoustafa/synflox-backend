using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PlanProjectModuleRepository : BaseRepository<Guid, PlanProjectModule>, IPlanProjectModuleRepository
{
    public PlanProjectModuleRepository(ApplicationDBContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PlanProjectModule>> GetByPlanIdAsync(Guid planId)
    {
        return await _dbSet
            .Where(p => p.SubscriptionPlanId == planId && !p.IsDeleted)
            .ToListAsync();
    }
}

