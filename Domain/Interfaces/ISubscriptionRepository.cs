using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces;

public interface ISubscriptionRepository : IBaseRepository<Guid, Subscription>
{
    Task<Subscription?> GetActiveByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetActiveByCompanyAndPlanAsync(Guid companyId, Guid planId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetExpiredSubscriptionsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsDueForActivationAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IEnumerable<Subscription>> GetSubscriptionsForAutoRenewalAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingActiveSubscriptionAsync(Guid companyId, Guid planId, DateTime startDate, DateTime expiryDate, Guid? excludeSubscriptionId = null, CancellationToken cancellationToken = default);
    Task<Subscription?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasActiveSubscriptionsForPlanAsync(Guid planId, CancellationToken cancellationToken = default);
}
