namespace Api.Abstractions;

public sealed record ProjectUsageId : IdBase<ProjectUsageId>, IId<ProjectUsageId>
{
    public ProjectUsageId(Guid value) : base(value)
    {
    }

    public static ProjectUsageId Create(Guid value)
        => new(value);
}
