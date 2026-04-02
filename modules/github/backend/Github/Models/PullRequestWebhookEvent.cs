using System.ComponentModel.DataAnnotations.Schema;
using Database.Models;
using Github.Abstractions;
using Octokit.Webhooks.Events;

namespace Github.Models;

public sealed class PullRequestWebhookEvent : BaseEntityWithId<PullRequestWebhookEventId>
{
    [Column(TypeName = "jsonb")]
    public required PullRequestEvent Event { get; set; }
    public DateTime? HandledAt { get; set; }
    public DateTime? ErroredAt { get; set; }
    public string? ErrorMessage { get; set; }
}
