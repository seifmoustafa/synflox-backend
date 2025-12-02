using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces;

public interface IProjectRepository : IBaseRepository<Guid, Project>
{
    Task<Project?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetWithModulesAsync(CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithModulesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInActiveSubscriptionsAsync(Guid projectId, CancellationToken cancellationToken = default);
    
    // Delete cascade support
    Task<int> GetAffectedPlansCountAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAffectedPlanNamesAsync(Guid projectId, int take = 10, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedRecordsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task RemoveFromAllPlansAsync(Guid projectId, CancellationToken cancellationToken = default);
}
