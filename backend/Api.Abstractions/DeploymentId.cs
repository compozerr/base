namespace Api.Abstractions;

public sealed record DeploymentId(Guid Value) : IdBase<DeploymentId>(Value), IId<DeploymentId>
{
    public static DeploymentId Create(Guid value)
        => new(value);
}