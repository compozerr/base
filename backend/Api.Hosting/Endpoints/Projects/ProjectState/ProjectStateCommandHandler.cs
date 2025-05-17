using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.ProjectState;

public sealed class ProjectStateCommandHandler(
	IProjectRepository projectRepository) : ICommandHandler<ProjectStateCommand, ProjectStateResponse>
{
	public async Task<ProjectStateResponse> Handle(
	    ProjectStateCommand command,
	    CancellationToken cancellationToken = default)
	{
		await projectRepository.SetProjectStateAsync(
			command.ProjectId,
			command.State);
		return new ProjectStateResponse();
	}
}
