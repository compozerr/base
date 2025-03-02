namespace Api.Abstractions;

public sealed record ProjectEnvironmentId : IdBase<ProjectEnvironmentId>, IId<ProjectEnvironmentId>
{
    public ProjectEnvironmentId(Guid value) : base(value)
    {
    }

    public static ProjectEnvironmentId Create(Guid value)
        => new(value);
}
