using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;

namespace Github.Repositories;

public interface IModuleSyncEventRepository
    : IGenericRepository<ModuleSyncEvent, ModuleSyncEventId, GithubDbContext>
{
    Task<IReadOnlyList<ModuleSyncEvent>> GetByPushWebhookEventIdAsync(
        PushWebhookEventId pushWebhookEventId);
}

public sealed class ModuleSyncEventRepository(
    GithubDbContext context)
    : GenericRepository<ModuleSyncEvent, ModuleSyncEventId, GithubDbContext>(context),
      IModuleSyncEventRepository
{
    public async Task<IReadOnlyList<ModuleSyncEvent>> GetByPushWebhookEventIdAsync(
        PushWebhookEventId pushWebhookEventId)
    {
        return await GetFilteredAsync(x => x.PushWebhookEventId == pushWebhookEventId);
    }
}
