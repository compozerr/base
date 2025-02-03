namespace Api.Abstractions;

public sealed record ModuleId : IdBase<ModuleId>, IId<ModuleId>
{
    public ModuleId(Guid value) : base(value)
    {
    }

    public static ModuleId Create(Guid value)
        => new(value);
}
