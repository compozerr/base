namespace Github.Abstractions;

public sealed record InstallationId : IdBase<InstallationId>, IId<InstallationId>
{
    public InstallationId(Guid Value) : base(Value)
    {
    }

    public static InstallationId Create(Guid value)
        => new(value);
}
