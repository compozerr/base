namespace Github.Abstractions;

public sealed record ModuleSyncEventId(Guid Value) : IdBase<ModuleSyncEventId>(Value), IId<ModuleSyncEventId>
{
    public static ModuleSyncEventId Create(Guid value)
        => new(value);
}
