namespace Api.Abstractions;

public sealed record ModuleRegistryEntryId : IdBase<ModuleRegistryEntryId>, IId<ModuleRegistryEntryId>
{
    public ModuleRegistryEntryId(Guid value) : base(value)
    {
    }

    public static ModuleRegistryEntryId Create(Guid value)
        => new(value);
}
