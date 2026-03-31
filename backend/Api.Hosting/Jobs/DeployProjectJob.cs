using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Api.Hosting.Services;
using Jobs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Hosting.Jobs;

public class DeployProjectJob(
    IHostingApiFactory hostingApiFactory,
    IDeploymentRepository deploymentRepository,
    IMediator mediator) : JobBase<DeployProjectJob, DeploymentId>
{
    public override async Task ExecuteAsync(DeploymentId deploymentId)
    {
        var deployment = await deploymentRepository.GetByIdAsync(
            deploymentId,
            d => d.Include(x => x.Project)
                  .Include(x => x.Environment)) ?? throw new ArgumentException("Deployment not found");

        deployment.Status = DeploymentStatus.Deploying;
        deployment.StartedAtUtc = DateTime.UtcNow;
        await deploymentRepository.UpdateAsync(deployment);

        try
        {
            var api = await hostingApiFactory.GetHostingApiAsync(
                deployment.Project!.ServerId ?? throw new ServerNotFoundException());

            await api.DeployAsync(deployment);
        }
        catch (GitHubTokenException ex)
        {
            Log.ForContext("DeploymentId", deploymentId.Value)
                .Error(ex, "Deployment failed due to GitHub token issue");

            await mediator.Send(new AddLogCommand(
                deploymentId,
                ex.Message,
                LogLevel.Error));

            await mediator.Send(new ChangeDeploymentStatusCommand(
                deploymentId,
                DeploymentStatus.Failed));

            // Don't rethrow - token errors won't resolve with retries
            return;
        }
        catch (ServerNotFoundException ex)
        {
            Log.ForContext("DeploymentId", deploymentId.Value)
                .Error(ex, "Deployment failed: no server assigned");

            await mediator.Send(new AddLogCommand(
                deploymentId,
                "No server assigned to this project. Please contact support.",
                LogLevel.Error));

            await mediator.Send(new ChangeDeploymentStatusCommand(
                deploymentId,
                DeploymentStatus.Failed));

            return;
        }
    }
}