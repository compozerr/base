using Octokit.Webhooks;
using Octokit.Webhooks.Events;

namespace Github.Services;

public sealed class DefaultWebhookEventProcessor : WebhookEventProcessor
{
    protected override Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent)
    {
        return base.ProcessPushWebhookAsync(headers, pushEvent);
    }
} 