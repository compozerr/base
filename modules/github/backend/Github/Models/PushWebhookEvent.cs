using System.ComponentModel.DataAnnotations.Schema;
using Database.Models;
using Github.Abstractions;
using Octokit.Webhooks.Events;

namespace Github.Models;

public sealed class PushWebhookEvent : BaseEntityWithId<PushWebhookEventId>
{
    [Column(TypeName = "jsonb")]
    public required PushEvent Event { get; set; }
    public DateTime? HandledAt { get; set; }
    public DateTime? ErroredAt { get; set; }
    public string? ErrorMessage { get; set; }
}