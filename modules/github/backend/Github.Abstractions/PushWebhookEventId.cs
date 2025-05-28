namespace Github.Abstractions;

public sealed record PushWebhookEventId(Guid Value) : IdBase<PushWebhookEventId>(Value), IId<PushWebhookEventId>
{
    public static PushWebhookEventId Create(Guid value)
        => new(value);
}
