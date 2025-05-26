namespace Api.Data.Extensions;

public static class DeploymentStatusFilterExtensions
{
    public static List<DeploymentStatus> ToStatuses(this DeploymentStatusFilter filter)
    {
        var states = new List<DeploymentStatus>();
        if ((filter & DeploymentStatusFilter.None) != 0) states.Add(DeploymentStatus.Unknown);
        if ((filter & DeploymentStatusFilter.Deploying) != 0) states.Add(DeploymentStatus.Deploying);
        if ((filter & DeploymentStatusFilter.Completed) != 0) states.Add(DeploymentStatus.Completed);
        if ((filter & DeploymentStatusFilter.Queued) != 0) states.Add(DeploymentStatus.Queued);
        if ((filter & DeploymentStatusFilter.Failed) != 0) states.Add(DeploymentStatus.Failed);
        return states;
    }
}
