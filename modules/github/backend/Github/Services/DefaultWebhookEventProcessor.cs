using Github.Jobs;
using Github.Repositories;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.PullRequest;
using Serilog;

namespace Github.Services;

public sealed class DefaultWebhookEventProcessor(
    IPushWebhookEventRepository pushWebhookEventRepository,
    IPullRequestWebhookEventRepository pullRequestWebhookEventRepository) : WebhookEventProcessor
{
    protected override async Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent)
    {
        var entity = await pushWebhookEventRepository.AddAsync(new() { Event = pushEvent });

        // Deploy pipeline (existing)
        PushWebhookProcessorJob.Enqueue(entity.Id);

        // Module sync pipeline (runs independently)
        ModuleSyncProcessorJob.Enqueue(entity.Id);
    }

    protected override async Task ProcessPullRequestWebhookAsync(
        WebhookHeaders headers,
        PullRequestEvent pullRequestEvent,
        PullRequestAction action)
    {
        Log.Information(
            "Received pull_request webhook with action {Action} for PR #{PrNumber}",
            action, pullRequestEvent.PullRequest?.Number);

        var entity = await pullRequestWebhookEventRepository.AddAsync(
            new() { Event = pullRequestEvent });

        PullRequestWebhookProcessorJob.Enqueue(entity.Id);
    }
}