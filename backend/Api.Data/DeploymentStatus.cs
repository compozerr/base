namespace Api.Data;

public enum DeploymentStatus
{
    Unknown = 0,
    Deploying = 1,
    Completed = 2,
    Queued = 3,
    Failed = 4
}
