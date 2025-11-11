using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;
using Domain.Enums;

namespace Domain.Interfaces;

public interface ISubscriptionPlanRepository : IBaseRepository<Guid, SubscriptionPlan>
{
    Task<SubscriptionPlan?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SubscriptionPlan>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<decimal?> GetPriceAsync(Guid planId, Currency currency, CancellationToken cancellationToken = default);
}
