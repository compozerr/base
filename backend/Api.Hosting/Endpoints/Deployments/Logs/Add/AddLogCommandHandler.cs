using System.Text.Json;
using Api.Abstractions;
using Core.MediatR;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var currentLog = await storageService.DownloadAsync(GetLogFileName(command.DeploymentId), cancellationToken) ?? new MemoryStream();

        var writer = new StreamWriter(currentLog);
        writer.WriteLine(GetLogEntry(command.Log, command.Level));
        writer.Flush();
        currentLog.Position = 0;

        await storageService.UploadAsync(GetLogFileName(command.DeploymentId), currentLog, cancellationToken);

        return new AddLogResponse();
    }

    private static string GetLogEntry(string log, LogLevel level)
        => $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpper()}] {log}";

    private static string GetLogFileName(DeploymentId deploymentId)
        => $"deployment-logs/{deploymentId}.log";
}
