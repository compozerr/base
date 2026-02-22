namespace Api.Abstractions;

public sealed record ModuleVersionId : IdBase<ModuleVersionId>, IId<ModuleVersionId>
{
    public ModuleVersionId(Guid value) : base(value)
    {
    }

    public static ModuleVersionId Create(Guid value)
        => new(value);
}
