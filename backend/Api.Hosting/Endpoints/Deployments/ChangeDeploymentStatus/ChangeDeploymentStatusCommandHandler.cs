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

        if (command.Status == DeploymentStatus.Failed)
            deployment.BuildDuration = deployment.GetBuildDuration();

        await deploymentRepository.UpdateAsync(
            deployment,
            cancellationToken);

        return new(true);
    }
}
