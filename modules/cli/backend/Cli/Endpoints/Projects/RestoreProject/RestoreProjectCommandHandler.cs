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

		return new RestoreProjectResponse();
	}
}
