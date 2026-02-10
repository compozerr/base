using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public sealed class ExecuteCommandCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<ExecuteCommandCommand, ExecuteCommandResponse>
{
    public async Task<ExecuteCommandResponse> Handle(ExecuteCommandCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken);

        if (project?.ServerId is not { } serverId)
            return new ExecuteCommandResponse(-1, "", "Project not found or has no server");

        var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
        var result = await hostingApi.ExecuteCommandAsync(command.ProjectId, command.Command);

        return new ExecuteCommandResponse(result.ExitCode, result.Stdout, result.Stderr);
    }
}
