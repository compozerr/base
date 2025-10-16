namespace Api.Abstractions;

public sealed record ProjectServiceId : IdBase<ProjectServiceId>, IId<ProjectServiceId>
{
    public ProjectServiceId(Guid value) : base(value)
    {
    }

    public static ProjectServiceId Create(Guid value)
        => new(value);
}
