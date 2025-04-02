using Api.Abstractions.Helpers;
using Core.MediatR;
using Storage;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

public sealed class GetLogCommandHandler(
    IStorageService storageService) : ICommandHandler<GetLogCommand, string>
{
    public async Task<string> Handle(GetLogCommand command, CancellationToken cancellationToken = default)
    {
        var logStream = await storageService.DownloadAsync(LogHelpers.GetLogFileName(command.DeploymentId), cancellationToken);
        if (logStream is null) return string.Empty;

        logStream.Position = 0; // Reset stream position to ensure we read from the beginning
        using var reader = new StreamReader(logStream);
        var logContent = await reader.ReadToEndAsync(cancellationToken);
        return logContent;
    }
}
