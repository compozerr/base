using Api.Abstractions;
using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Core.Extensions;
using Core.MediatR;
using MediatR;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed class ChangeDeploymentStatusCommandHandler(
    IDeploymentRepository deploymentRepository,
    IMediator mediator) : ICommandHandler<ChangeDeploymentStatusCommand, ChangeDeploymentStatusResponse>
{
    public async Task<ChangeDeploymentStatusResponse> Handle(ChangeDeploymentStatusCommand command, CancellationToken cancellationToken = default)
    {
        var deployment = (await deploymentRepository.GetByIdAsync(command.DeploymentId, cancellationToken))!;

        deployment.Status = command.Status;

        switch (command.Status)
        {
            case DeploymentStatus.Failed:
            case DeploymentStatus.Completed:
                deployment.BuildDuration = deployment.GetBuildDuration();
                await mediator.Send(
                    new DeploymentTriggerCommand(deployment.ProjectId),
                    cancellationToken);
                break;
            default:
                break;
        }

        await deploymentRepository.UpdateAsync(
            deployment,
            cancellationToken);

        return new(true);
    }
}
