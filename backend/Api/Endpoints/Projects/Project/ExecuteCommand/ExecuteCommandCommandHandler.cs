using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Project.ExecuteCommand;

public sealed class ExecuteCommandCommandHandler(
    ApiDbContext context,
    IEnvironmentTargetResolver environmentTargetResolver) : ICommandHandler<ExecuteCommandCommand, ExecuteCommandResponse>
{
    public async Task<ExecuteCommandResponse> Handle(ExecuteCommandCommand command, CancellationToken cancellationToken = default)
    {
        // Resolve env: by name if provided, else the project's production env
        // (matches the prior behaviour of "talk to the project's main VM").
        var envQuery = context.ProjectEnvironments
            .Where(e => e.ProjectId == command.ProjectId);

        var envId = command.EnvironmentName is { } name
            ? await envQuery.Where(e => e.Name == name).Select(e => (ProjectEnvironmentId?)e.Id).FirstOrDefaultAsync(cancellationToken)
            : await envQuery.Where(e => e.Type == EnvironmentType.Production).Select(e => (ProjectEnvironmentId?)e.Id).FirstOrDefaultAsync(cancellationToken);

        if (envId is null)
            return new ExecuteCommandResponse(-1, "", "Project not found or has no environment");

        var server = await environmentTargetResolver.TryGetServerAsync(envId, cancellationToken);
        if (server is null)
            return new ExecuteCommandResponse(-1, "", "Project not found or has no server");

        var hostingApi = await environmentTargetResolver.GetHostingApiAsync(envId, cancellationToken);
        var result = await hostingApi.ExecuteCommandAsync(command.ProjectId, command.Command, command.EnvironmentName);

        return new ExecuteCommandResponse(result.ExitCode, result.Stdout, result.Stderr);
    }
}
