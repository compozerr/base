using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment.ChangeAutoDeploy;

public sealed class ChangeAutoDeployCommandHandler(
	IProjectEnvironmentRepository projectEnvironmentRepository) : ICommandHandler<ChangeAutoDeployCommand, ChangeAutoDeployResponse>
{
	public async Task<ChangeAutoDeployResponse> Handle(ChangeAutoDeployCommand command, CancellationToken cancellationToken = default)
	{
		var projectEnvironment = await projectEnvironmentRepository.GetProjectEnvironmentByBranchAsync(command.ProjectId, "main") ?? throw new ArgumentException($"Project environment with ID {command.ProjectId} not found.");
		
        projectEnvironment.AutoDeploy = command.AutoDeploy;

		await projectEnvironmentRepository.UpdateAsync(projectEnvironment, cancellationToken);

		return new ChangeAutoDeployResponse();
	}
}
