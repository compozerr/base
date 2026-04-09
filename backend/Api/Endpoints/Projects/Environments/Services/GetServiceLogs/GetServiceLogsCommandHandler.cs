using System.Text.RegularExpressions;
using Api.Data;
using Api.Hosting.Services;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Services.GetServiceLogs;

public sealed partial class GetServiceLogsCommandHandler(
    ApiDbContext context,
    IHostingApiFactory hostingApiFactory)
    : ICommandHandler<GetServiceLogsCommand, GetServiceLogsResponse>
{
    [GeneratedRegex(@"^[a-zA-Z0-9_\-]+$")]
    private static partial Regex SafeServiceNameRegex();

    public async Task<GetServiceLogsResponse> Handle(
        GetServiceLogsCommand command,
        CancellationToken cancellationToken)
    {
        if (!SafeServiceNameRegex().IsMatch(command.ServiceName))
            return new GetServiceLogsResponse("Invalid service name.", -1);

        var project = await context.Projects
            .Where(p => p.Id == command.ProjectId && p.ServerId != null)
            .Select(p => new { p.ServerId })
            .FirstOrDefaultAsync(cancellationToken);

        if (project?.ServerId is null)
            return new GetServiceLogsResponse("Project has no server assigned.", -1);

        var environment = await context.ProjectEnvironments
            .Where(e => e.Id == command.EnvironmentId && e.ProjectId == command.ProjectId)
            .Select(e => new { e.Name, e.Type })
            .FirstOrDefaultAsync(cancellationToken);

        if (environment is null)
            return new GetServiceLogsResponse("Environment not found.", -1);

        var tail = Math.Clamp(command.Tail, 10, 1000);
        var dockerCommand = $"docker compose logs --tail {tail} --no-color {command.ServiceName}";

        // Production environments don't pass an environment name (they use the main VM)
        var environmentName = environment.Type == EnvironmentType.Production ? null : environment.Name;

        var hostingApi = await hostingApiFactory.GetHostingApiAsync(project.ServerId);
        var result = await hostingApi.ExecuteCommandAsync(command.ProjectId, dockerCommand, environmentName);

        var logs = !string.IsNullOrEmpty(result.Stdout) ? result.Stdout : result.Stderr;
        return new GetServiceLogsResponse(logs, result.ExitCode);
    }
}
