using System;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for SubscriptionPlan entity operations.
/// </summary>
public interface ISubscriptionPlanRepository : IBaseRepository<Guid, SubscriptionPlan>
{
    // Custom methods can be added here if needed
}

