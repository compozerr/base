using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;

namespace Github.Repositories;

public interface IPushWebhookEventRepository : IGenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>;

public sealed class PushWebhookEventRepository(
    GithubDbContext context) : GenericRepository<PushWebhookEvent, PushWebhookEventId, GithubDbContext>(context), IPushWebhookEventRepository;