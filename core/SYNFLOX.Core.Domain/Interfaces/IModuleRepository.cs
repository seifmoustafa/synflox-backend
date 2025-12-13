using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Subscriptions;

namespace Domain.Interfaces;

public interface IModuleRepository : IBaseRepository<Guid, Module>
{
    Task<Module?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Module>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInProjectsOrPlansAsync(Guid moduleId, CancellationToken cancellationToken = default);
    
    // Delete cascade support
    Task<int> GetAffectedProjectsCountAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAffectedProjectNamesAsync(Guid moduleId, int take = 10, CancellationToken cancellationToken = default);
    Task<int> GetAffectedPlansCountAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAffectedPlanNamesAsync(Guid moduleId, int take = 10, CancellationToken cancellationToken = default);
    Task<bool> HasRelatedRecordsAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task RemoveFromAllProjectsAsync(Guid moduleId, CancellationToken cancellationToken = default);
    Task RemoveFromAllPlansAsync(Guid moduleId, CancellationToken cancellationToken = default);
}
