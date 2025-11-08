using System;
using Domain.Entities.Licensing;
using Domain.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories;

public class SubscriptionPlanRepository : BaseRepository<Guid, SubscriptionPlan>, ISubscriptionPlanRepository
{
    public SubscriptionPlanRepository(ApplicationDBContext context) : base(context)
    {
    }
}

