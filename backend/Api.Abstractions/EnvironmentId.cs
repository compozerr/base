namespace Api.Abstractions;

public sealed record EnvironmentId : IdBase<EnvironmentId>, IId<EnvironmentId>
{
    public EnvironmentId(Guid value) : base(value)
    {
    }

    public static EnvironmentId Create(Guid value)
        => new(value);
}
