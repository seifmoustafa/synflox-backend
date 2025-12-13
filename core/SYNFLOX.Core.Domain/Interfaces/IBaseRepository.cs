using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Common;

namespace Domain.Interfaces
{
    public interface IBaseRepository<TKey, TEntity> where TKey : struct where TEntity : BaseEntity<TKey>
    {
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntity>> GetAllAsync(string[]? includes, CancellationToken cancellationToken = default);

        Task<TEntity?> GetByIdAsync(TKey id, string[]? includes, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

        //TODO: Pagination
        //TODO : Sorting , Filtering , isDeleted Filter
        #region Pagination
        Task<(IEnumerable<TEntity>, PaginationMetadata)> GetAllAsync(string[]? includes, int pageNumber = 1, int pageSize = 10,
            string? search = null, CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object?>>[] searchColumns);

        Task<(IEnumerable<TEntity>, PaginationMetadata)> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
            string[]? includes, int pageNumber = 1, int pageSize = 10, string? search = null,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object?>>[] searchColumns);
        #endregion
        Task<int> Count(CancellationToken cancellationToken = default);
    }
}
