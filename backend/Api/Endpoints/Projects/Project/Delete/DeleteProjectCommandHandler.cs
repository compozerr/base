using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Should not be able to be here if null");

        if (project.ServerId is { } serverId)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            await hostingApi.DeleteProjectAsync(project.Id);
        }

        await projectRepository.DeleteAsync(project.Id, cancellationToken);

        return new();
    }
}
