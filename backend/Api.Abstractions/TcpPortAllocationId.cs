namespace Api.Abstractions;

public sealed record TcpPortAllocationId : IdBase<TcpPortAllocationId>, IId<TcpPortAllocationId>
{
    public TcpPortAllocationId(Guid value) : base(value)
    {
    }

    public static TcpPortAllocationId Create(Guid value)
        => new(value);
}
