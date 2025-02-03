namespace Api.Abstractions;

public sealed record DeploymentId : IdBase<DeploymentId>, IId<DeploymentId>
{
    public DeploymentId(Guid value) : base(value)
    {
    }

    public static DeploymentId Create(Guid value)
        => new(value);
}
