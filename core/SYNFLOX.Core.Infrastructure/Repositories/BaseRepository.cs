using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities.Common;
using Domain.Interfaces;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class BaseRepository<TKey, TEntity> : IBaseRepository<TKey, TEntity>
      where TKey : struct where TEntity : BaseEntity<TKey>
    {
        protected readonly ApplicationDBContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public BaseRepository(ApplicationDBContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Filter soft-deleted entities - BaseEntity always has IsDeleted
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(string[]? includes, CancellationToken cancellationToken = default)
        {
            // Apply filters before includes
            var query = _dbSet
                .Where(e => !e.IsDeleted)
                .AsQueryable();
            
            // Apply includes
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            return await query.AsNoTracking().ToListAsync(cancellationToken);
        }

        public async Task<TEntity?> GetByIdAsync(TKey id, string[]? includes, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(e => !e.IsDeleted && EF.Property<TKey>(e, "Id").Equals(id))
                .AsQueryable();
            
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);
            
            return await query.FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .Where(predicate)
                .ToListAsync(cancellationToken);
        }


        public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            // Check if the entity's key is of type Guid
            if (typeof(TKey) == typeof(Guid))
            {
                // Generate a new Guid if the Id is empty
                var propertyInfo = entity.GetType().GetProperty("Id");
                if (propertyInfo != null && propertyInfo.GetValue(entity) is Guid id && id == Guid.Empty)
                {
                    propertyInfo.SetValue(entity, Guid.NewGuid());
                }
            }

            await _dbSet.AddAsync(entity, cancellationToken);

            // Return the newly added entity (caller must call SaveChangesAsync)
            return entity;
        }

        public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            // Caller must call SaveChangesAsync
        }


        public async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        {
            // Soft delete: Set IsDeleted flag instead of removing from database
            var entity = await _dbSet
                .FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id").Equals(id) && !e.IsDeleted, cancellationToken);
            
            if (entity != null)
            {
                entity.IsDeleted = true;
                if (entity is AuditEntity<TKey> auditEntity)
                {
                    auditEntity.DeletedTimestamp = DateTime.UtcNow;
                }
                _dbSet.Update(entity);
                // Caller must call SaveChangesAsync
            }
        }

        public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (typeof(TKey) == typeof(Guid))
            {
                foreach (var entity in entities)
                {
                    var propertyInfo = entity.GetType().GetProperty("Id");
                    if (propertyInfo != null && propertyInfo.GetValue(entity) is Guid id && id == Guid.Empty)
                    {
                        propertyInfo.SetValue(entity, Guid.NewGuid());
                    }
                }
            }

            await _dbSet.AddRangeAsync(entities, cancellationToken);
            // Caller must call SaveChangesAsync

            return entities;
        }

        public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(entities);
            // Caller must call SaveChangesAsync
        }

        public async Task DeleteRangeAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            var entities = await _dbSet
                .Where(e => !e.IsDeleted && ids.Contains(EF.Property<TKey>(e, "Id")))
                .ToListAsync(cancellationToken);
            if (entities.Any())
            {
                _dbSet.RemoveRange(entities);
                // Caller must call SaveChangesAsync
            }
        }


        public Task<int> Count(CancellationToken cancellationToken = default)
        {
            return _dbSet
                .Where(e => !e.IsDeleted)
                .CountAsync(cancellationToken);
        }



        #region Pagination
        public async Task<(IEnumerable<TEntity>, PaginationMetadata)> GetAllAsync(string[]? includes, int pageNumber = 1,
            int pageSize = 10, string? search = null, CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object?>>[] searchColumns)
        {
            // Apply filters before includes to reduce data loaded
            var query = _dbSet
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            // Apply search filter early to reduce dataset
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(GenerateSearchExpression(search, searchColumns));
            }

            // Optimize: Count before includes (faster)
            var itemsCount = await query.CountAsync(cancellationToken);

            // Apply includes only for the final query
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            // Use AsNoTracking for read-only queries
            query = query.AsNoTracking();

            // Optimize: Apply pagination at database level
            var paginationMetadata = new PaginationMetadata(itemsCount, pageSize, pageNumber);

            var result = await query
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (result, paginationMetadata);
        }

        public async Task<(IEnumerable<TEntity>, PaginationMetadata)> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
            string[]? includes, int pageNumber = 1, int pageSize = 10, string? search = null,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object?>>[] searchColumns)
        {
            // Apply filters before includes
            var query = _dbSet
                .Where(e => !e.IsDeleted)
                .AsQueryable();

            // Apply predicate early
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            // Apply search filter early
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(GenerateSearchExpression(search, searchColumns));
            }

            // Optimize: Count before includes
            var itemsCount = await query.CountAsync(cancellationToken);

            // Apply includes only for final query
            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            query = query.AsNoTracking();

            var paginationMetadata = new PaginationMetadata(itemsCount, pageSize, pageNumber);

            // Optimize: Apply pagination at database level
            var result = await query
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (result, paginationMetadata);
        }

        private static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions)
            .GetMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

        private static Expression<Func<TEntity, bool>> GenerateSearchExpression(string search,
            Expression<Func<TEntity, object?>>[]? searchColumns = null)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");

            Expression? searchExpression = null;

            if (searchColumns == null || searchColumns.Length == 0)
            {
                var properties = typeof(TEntity).GetProperties()
                    .Where(p => p.CanRead &&
                        (p.PropertyType == typeof(string) ||
                         (p.PropertyType.IsValueType &&
                          !(Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType).IsEnum)));

                foreach (var property in properties)
                {
                    Expression propertyExpr = Expression.Property(parameter, property);

                    if (property.PropertyType != typeof(string))
                    {
                        propertyExpr = Expression.Call(propertyExpr,
                            property.PropertyType.GetMethod(nameof(object.ToString), Type.EmptyTypes)!);
                    }

                    var likeCall = Expression.Call(
                        LikeMethod,
                        Expression.Constant(EF.Functions),
                        propertyExpr,
                        Expression.Constant($"%{search}%"));

                    searchExpression = searchExpression == null
                        ? likeCall
                        : Expression.OrElse(searchExpression, likeCall);
                }
            }
            else
            {
                foreach (var column in searchColumns)
                {
                    Expression body = column.Body;

                    if (body.NodeType == ExpressionType.Convert && body is UnaryExpression unary)
                        body = unary.Operand;

                    // Replace parameter of the column with the method parameter
                    var replaced = new ReplaceParameterVisitor(column.Parameters[0], parameter).Visit(body)!;

                    if (replaced.Type != typeof(string))
                    {
                        replaced = Expression.Call(replaced, replaced.Type.GetMethod(nameof(object.ToString), Type.EmptyTypes)!);
                    }

                    var likeCall = Expression.Call(
                        LikeMethod,
                        Expression.Constant(EF.Functions),
                        replaced,
                        Expression.Constant($"%{search}%"));

                    searchExpression = searchExpression == null
                        ? likeCall
                        : Expression.OrElse(searchExpression, likeCall);
                }
            }

            return searchExpression != null
                ? Expression.Lambda<Func<TEntity, bool>>(searchExpression, parameter)
                : entity => true;
        }

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParameter;
            private readonly Expression _newExpression;

            public ReplaceParameterVisitor(ParameterExpression oldParameter, Expression newExpression)
            {
                _oldParameter = oldParameter;
                _newExpression = newExpression;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _oldParameter ? _newExpression : base.VisitParameter(node);
        }

        #endregion
    }
}
