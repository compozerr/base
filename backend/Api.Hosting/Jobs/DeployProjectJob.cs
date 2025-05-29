using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Jobs;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Jobs;

public class DeployProjectJob(
    IHostingApiFactory hostingApiFactory,
    IDeploymentRepository deploymentRepository) : JobBase<DeployProjectJob, DeploymentId>
{
    public override async Task ExecuteAsync(DeploymentId deploymentId)
    {
        var deployment = await deploymentRepository.GetByIdAsync(
            deploymentId,
            d => d.Include(x => x.Project)) ?? throw new ArgumentException("Deployment not found");

        deployment.Status = Data.DeploymentStatus.Deploying;

        await deploymentRepository.UpdateAsync(deployment);

        var api = await hostingApiFactory.GetHostingApiAsync(deployment.Project!.ServerId ?? throw new ServerNotFoundException());

        await api.DeployAsync(deployment);
    }
}