using Core.Abstractions;

namespace Mail;

public sealed record SentEmailId : IdBase<SentEmailId>, IId<SentEmailId>
{
    public SentEmailId(Guid value) : base(value)
    {
    }

    public static SentEmailId Create(Guid value)
        => new(value);
}
