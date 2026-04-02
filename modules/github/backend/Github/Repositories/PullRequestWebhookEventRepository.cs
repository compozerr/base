using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;

namespace Github.Repositories;

public interface IPullRequestWebhookEventRepository : IGenericRepository<PullRequestWebhookEvent, PullRequestWebhookEventId, GithubDbContext>;

public sealed class PullRequestWebhookEventRepository(
    GithubDbContext context) : GenericRepository<PullRequestWebhookEvent, PullRequestWebhookEventId, GithubDbContext>(context), IPullRequestWebhookEventRepository;
