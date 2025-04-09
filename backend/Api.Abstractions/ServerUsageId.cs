namespace Api.Abstractions;

public sealed record ServerUsageId : IdBase<ServerUsageId>, IId<ServerUsageId>
{
    public ServerUsageId(Guid value) : base(value)
    {
    }

    public static ServerUsageId Create(Guid value)
        => new(value);
}
