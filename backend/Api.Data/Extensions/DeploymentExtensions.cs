namespace Api.Data.Extensions;

public static class DeploymentExtensions
{
    public static TimeSpan GetBuildDuration(this Deployment deployment)
        => deployment.BuildDuration ?? (DateTime.UtcNow - deployment.CreatedAtUtc);
}