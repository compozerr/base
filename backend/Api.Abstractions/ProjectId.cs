namespace Api.Abstractions;

public sealed record ProjectId : IdBase<ProjectId>, IId<ProjectId>
{
    public ProjectId(Guid value) : base(value)
    {
    }

    public static ProjectId Create(Guid value)
        => new(value);
}
