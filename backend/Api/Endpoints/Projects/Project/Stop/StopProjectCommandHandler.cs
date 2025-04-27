using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Stop;

public sealed class StopProjectCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<StopProjectCommand, StopProjectResponse>
{
    public async Task<StopProjectResponse> Handle(StopProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken);

        if (project?.ServerId is { } serverId)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            await hostingApi.StopProjectAsync(command.ProjectId);
        }

        return new();
    }
}
