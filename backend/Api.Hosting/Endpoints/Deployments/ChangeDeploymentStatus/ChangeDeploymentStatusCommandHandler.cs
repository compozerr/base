using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed class ChangeDeploymentStatusCommandHandler(
    IDeploymentRepository deploymentRepository,
    IProjectRepository projectRepository,
    IMediator mediator) : ICommandHandler<ChangeDeploymentStatusCommand, ChangeDeploymentStatusResponse>
{
    public async Task<ChangeDeploymentStatusResponse> Handle(ChangeDeploymentStatusCommand command, CancellationToken cancellationToken = default)
    {
        var deployment = (await deploymentRepository.GetByIdAsync(command.DeploymentId, cancellationToken))!;

        deployment.Status = command.Status;

        switch (command.Status)
        {
            case DeploymentStatus.Completed:
                await projectRepository.SetProjectStateAsync(deployment.ProjectId, ProjectState.Running);
                deployment.BuildDuration = deployment.GetBuildDuration();
                break;
            case DeploymentStatus.Failed:
                deployment.BuildDuration = deployment.GetBuildDuration();
                break;
            default:
                break;
        }

        await deploymentRepository.UpdateAsync(
            deployment,
            cancellationToken);

        await mediator.Send(
                new DeploymentTriggerCommand(deployment.ProjectId),
                cancellationToken);

        return new(true);
    }
}
