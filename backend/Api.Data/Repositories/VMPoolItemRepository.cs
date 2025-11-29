using Auth.Abstractions;
using Database.Repositories;

namespace Api.Data.Repositories;

public interface IVMPoolItemRepository : IGenericRepository<VMPoolItem, VMPoolItemId, ApiDbContext>
{
    Task MarkAsDelegatedAsync(
        VMPoolItem vmPoolItem,
        UserId userId,
        DateTime delegatedAt);
}

public sealed class VMPoolItemRepository(
    ApiDbContext context) : GenericRepository<VMPoolItem, VMPoolItemId, ApiDbContext>(context), IVMPoolItemRepository
{
    public override IQueryable<VMPoolItem> Query(bool getDeleted = false)
        => base.Query(getDeleted)
               .Where(x => x.DelegatedAt == null);
    
    public async Task MarkAsDelegatedAsync(
        VMPoolItem vmPoolItem,
        UserId userId,
        DateTime delegatedAt)
    {
        vmPoolItem.DelegatedAt = delegatedAt;
        vmPoolItem.DelegatedToUserId = userId;

        await UpdateAsync(vmPoolItem);
    }
}