using Api.Data;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;

public sealed class ChangeDeploymentStatusCommandHandler(
    IDeploymentRepository deploymentRepository) : ICommandHandler<ChangeDeploymentStatusCommand, ChangeDeploymentStatusResponse>
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
