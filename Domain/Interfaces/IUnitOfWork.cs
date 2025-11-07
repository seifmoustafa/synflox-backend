using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    /// <summary>
    /// Provides a simple unit of work abstraction to execute multiple repository
    /// operations within a single database transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

