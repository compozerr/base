using Core.Abstractions;
using System.Linq.Expressions;

namespace Database.Repositories;

public interface IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntityWithId<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext<TDbContext>
{
    // Original methods
    ValueTask<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntityId id, CancellationToken cancellationToken = default);

    // Basic query methods
    IQueryable<TEntity> Query();

    // Methods with include builder pattern
    Task<TEntity?> GetByIdAsync(TEntityId id, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetFilteredAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default);
    Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default);

    // Filtered query methods
    Task<List<TEntity>> GetFilteredAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);
    Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    // Exists check
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    // Count methods
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

    // Custom query execution
    Task<List<TResult>> ExecuteQueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default);
    Task<TResult?> ExecuteSingleQueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default);
}

public class GenericRepository<TEntity, TEntityId, TDbContext>(TDbContext context) : IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntityWithId<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext<TDbContext>
{
    // Original methods implementation
    public virtual ValueTask<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default)
        => new(context.Set<TEntity>().Where(
            e => e.DeletedAtUtc == null
                 && e.Id.Equals(id)
        ).FirstOrDefaultAsync(cancellationToken));

    public virtual Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Set<TEntity>().Where(
            e => e.DeletedAtUtc == null
        ).ToListAsync(cancellationToken);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        context.Set<TEntity>().UpdateRange(entities);
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken) ?? throw new Exception($"{typeof(TEntity).Name} with id {id} not found");

        entity.DeletedAtUtc = DateTime.UtcNow;

        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    // Basic query method implementation
    public virtual IQueryable<TEntity> Query()
        => context.Set<TEntity>().Where(
            e => e.DeletedAtUtc == null
        ).AsQueryable();

    // Methods with include builder pattern implementation
    public virtual async Task<TEntity?> GetByIdAsync(TEntityId id, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default)
    {
        var query = includeBuilder(Query());
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default)
    {
        var query = includeBuilder(Query());
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetFilteredAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default)
    {
        var query = includeBuilder(Query());
        return await query.Where(filter).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IQueryable<TEntity>> includeBuilder, CancellationToken cancellationToken = default)
    {
        var query = includeBuilder(Query());
        return await query.FirstOrDefaultAsync(filter, cancellationToken);
    }

    // Filtered query methods implementation
    public virtual Task<List<TEntity>> GetFilteredAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return Query().Where(filter).ToListAsync(cancellationToken);
    }

    public virtual Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return Query().FirstOrDefaultAsync(filter, cancellationToken);
    }

    // Exists check implementation
    public virtual Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return Query().AnyAsync(filter, cancellationToken);
    }

    // Count methods implementation
    public virtual Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Query().CountAsync(cancellationToken);
    }

    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
    {
        return Query().CountAsync(filter, cancellationToken);
    }

    // Custom query execution implementation
    public virtual Task<List<TResult>> ExecuteQueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default)
    {
        return query(Query()).ToListAsync(cancellationToken);
    }

    public virtual Task<TResult?> ExecuteSingleQueryAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default)
    {
        return query(Query()).FirstOrDefaultAsync(cancellationToken);
    }
}