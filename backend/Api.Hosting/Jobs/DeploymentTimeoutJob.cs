using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Hosting.Jobs;

public class DeploymentTimeoutJob(
    IDeploymentRepository deploymentRepository,
    IMediator mediator) : JobBase<DeploymentTimeoutJob>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);

    public override async Task ExecuteAsync()
    {
        var cutoff = DateTime.UtcNow - Timeout;

        var stuckDeployments = await deploymentRepository
            .Query()
            .Where(d => d.Status == DeploymentStatus.Deploying
                        && (d.UpdatedAtUtc ?? d.CreatedAtUtc) < cutoff)
            .ToListAsync();

        if (stuckDeployments.Count == 0)
            return;

        Log.Logger.Information(
            "DeploymentTimeoutJob found {Count} stuck deployment(s)", stuckDeployments.Count);

        foreach (var deployment in stuckDeployments)
        {
            Log.Logger.Information(
                "Timing out deployment {DeploymentId} for project {ProjectId}",
                deployment.Id, deployment.ProjectId);

            await mediator.Send(new AddLogCommand(
                deployment.Id,
                "Stopped by timeout job",
                LogLevel.Error));

            await mediator.Send(new ChangeDeploymentStatusCommand(
                deployment.Id,
                DeploymentStatus.Failed));

            var hasQueuedDeployments = await deploymentRepository
                .GetDeploymentsForProject(deployment.ProjectId)
                .AnyAsync(d => d.Status == DeploymentStatus.Queued);

            if (!hasQueuedDeployments)
            {
                Log.Logger.Information(
                    "No queued deployments for project {ProjectId}, triggering redeploy",
                    deployment.ProjectId);

                await mediator.Send(new DeployProjectCommand(
                    deployment.ProjectId,
                    deployment.CommitHash,
                    deployment.CommitMessage,
                    deployment.CommitAuthor,
                    deployment.CommitBranch,
                    deployment.CommitEmail,
                    OverrideAuthorization: true,
                    EnvironmentId: deployment.EnvironmentId));
            }
        }
    }
}
