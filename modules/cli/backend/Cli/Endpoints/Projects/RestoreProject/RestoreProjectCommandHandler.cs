using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Projects.RestoreProject;

public sealed class RestoreProjectCommandHandler(
	IProjectRepository projectRepository) : ICommandHandler<RestoreProjectCommand, RestoreProjectResponse>
{
	public async Task<RestoreProjectResponse> Handle(
		RestoreProjectCommand command,
		CancellationToken cancellationToken = default)
	{
		await projectRepository.RestoreAsync(
			command.ProjectId,
			cancellationToken);

		var project = (await projectRepository.GetByIdAsync(
		    command.ProjectId,
		    cancellationToken))!;

		project.State = Api.Data.ProjectState.Stopped;

		await projectRepository.UpdateAsync(
			project,
			cancellationToken);

		return new RestoreProjectResponse();
	}
}
