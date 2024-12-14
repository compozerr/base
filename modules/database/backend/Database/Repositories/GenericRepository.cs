using Core.Abstractions;

namespace Database.Repositories;

public interface IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntityWithId<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext
{
    ValueTask<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntityId id, CancellationToken cancellationToken = default);
}

public class GenericRepository<TEntity, TEntityId, TDbContext>(TDbContext context) : IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntityWithId<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext
{
    public virtual ValueTask<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default)
        => context.Set<TEntity>().FindAsync([id], cancellationToken);

    public virtual Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => context.Set<TEntity>().ToListAsync(cancellationToken);

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        context.Entry(entity).State = EntityState.Modified;
        await context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntityId id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken) ?? throw new Exception($"{typeof(TEntity).Name} with id {id} not found");
        context.Set<TEntity>().Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
    }
}