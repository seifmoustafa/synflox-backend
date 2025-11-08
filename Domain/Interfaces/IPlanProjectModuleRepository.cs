using Domain.Entities.Licensing;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Licensing;

namespace Domain.Interfaces;

/// <summary>
/// Repository interface for PlanProjectModule entity operations.
/// </summary>
public interface IPlanProjectModuleRepository : IBaseRepository<Guid, PlanProjectModule>
{
    /// <summary>
    /// Gets all plan-project-module associations for a subscription plan.
    /// </summary>
    Task<IEnumerable<PlanProjectModule>> GetByPlanIdAsync(Guid planId);
}

