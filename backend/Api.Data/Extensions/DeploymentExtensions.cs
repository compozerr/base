namespace Api.Data.Extensions;

public static class DeploymentExtensions
{
    public static TimeSpan GetBuildDuration(this Deployment deployment)
        => deployment.BuildDuration ?? (DateTime.UtcNow - (deployment.StartedAtUtc ?? deployment.CreatedAtUtc));

    public static string ResolveUrl(this Deployment deployment)
        => deployment.Environment?.GetUrl()
           ?? deployment.Project?.Domains?.GetPrimary()?.GetValue
           ?? "unknown";
}