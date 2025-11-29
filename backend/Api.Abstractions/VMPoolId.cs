namespace Api.Abstractions;

public sealed record VMPoolId : IdBase<VMPoolId>, IId<VMPoolId>
{
    public VMPoolId(Guid value) : base(value)
    {
    }

    public static VMPoolId Create(Guid value)
        => new(value);
}
