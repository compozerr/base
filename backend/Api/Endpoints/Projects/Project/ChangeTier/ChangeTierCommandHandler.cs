using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed class ChangeTierCommandHandler(
	IProjectRepository projectRepository,
	IHostingApiFactory hostingApiFactory) : ICommandHandler<ChangeTierCommand, ChangeTierResponse>
{
	public async Task<ChangeTierResponse> Handle(ChangeTierCommand command, CancellationToken cancellationToken = default)
	{
		var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ??
			throw new InvalidOperationException($"Project with ID {command.ProjectId} not found.");

		await ChangeTierOnServerAsync(project, command, cancellationToken);

		await ChangeTierOnEntityAsync(project, command, cancellationToken);

		return new ChangeTierResponse();
	}

	private async Task ChangeTierOnServerAsync(
		Data.Project project,
		ChangeTierCommand command,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(project.ServerId);

		var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId);

		
	}

	private async Task ChangeTierOnEntityAsync(
		Data.Project project,
		ChangeTierCommand command,
		CancellationToken cancellationToken)
	{


		project.ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id;

		await projectRepository.UpdateAsync(project, cancellationToken);
	}
}
