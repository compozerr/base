using Github.Jobs;
using Github.Repositories;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;

namespace Github.Services;

public sealed class DefaultWebhookEventProcessor(
    IPushWebhookEventRepository pushWebhookEventRepository) : WebhookEventProcessor
{
    protected override async Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent)
    {
        var entity = await pushWebhookEventRepository.AddAsync(new() { Event = pushEvent });

        PushWebhookProcessorJob.Enqueue(entity.Id);
    }
}