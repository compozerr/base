using Core.Abstractions;
using Database.Models;

namespace Database.Repositories;

public interface IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntity<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(int id);
}

public class GenericRepository<TEntity, TEntityId, TDbContext>(TDbContext context) : IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntity<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext
{
    protected readonly TDbContext _context = context;

    public virtual async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _context.Set<TEntity>().ToListAsync();
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id) ?? throw new Exception($"{typeof(TEntity).Name} with id {id} not found");
        _context.Set<TEntity>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}