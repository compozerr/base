using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.Extensions;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory,
    IDeploymentRepository deploymentRepository) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Should not be able to be here if null");

        if (project.ServerId is { } serverId)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            await hostingApi.DeleteProjectAsync(project.Id);
        }

        var deployingDeployments = await deploymentRepository.GetDeploymentsForProject(project.Id)
            .Where(d => d.Status == Data.DeploymentStatus.Queued || d.Status == Data.DeploymentStatus.Deploying)
            .ToListAsync(cancellationToken);

        if (deployingDeployments is { })
        {
            deployingDeployments.Apply(d => d.Status = Data.DeploymentStatus.Cancelled);
            await deploymentRepository.UpdateRangeAsync(deployingDeployments, cancellationToken);
        }

        await projectRepository.DeleteAsync(project.Id, cancellationToken);

        return new();
    }
}
