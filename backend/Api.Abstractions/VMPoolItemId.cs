namespace Api.Abstractions;

public sealed record VMPoolItemId : IdBase<VMPoolItemId>, IId<VMPoolItemId>
{
    public VMPoolItemId(Guid value) : base(value)
    {
    }

    public static VMPoolItemId Create(Guid value)
        => new(value);
}
