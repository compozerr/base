namespace Api.Abstractions;

public sealed record ProjectEnvironmentVariableId : IdBase<ProjectEnvironmentVariableId>, IId<ProjectEnvironmentVariableId>
{
    public ProjectEnvironmentVariableId(Guid value) : base(value)
    {
    }

    public static ProjectEnvironmentVariableId Create(Guid value)
        => new(value);
}
