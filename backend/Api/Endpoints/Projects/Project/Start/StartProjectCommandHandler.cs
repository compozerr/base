using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.Start;

public sealed class StartProjectCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<StartProjectCommand, StartProjectResponse>
{
    public async Task<StartProjectResponse> Handle(StartProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken);

        if (project?.ServerId is { } serverId)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            await hostingApi.StartProjectAsync(command.ProjectId);
        }

        return new();
    }
}
