namespace Github.Abstractions;

public sealed record PullRequestWebhookEventId(Guid Value) : IdBase<PullRequestWebhookEventId>(Value), IId<PullRequestWebhookEventId>
{
    public static PullRequestWebhookEventId Create(Guid value)
        => new(value);
}
