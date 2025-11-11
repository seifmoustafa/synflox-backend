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
}
