using Api.Abstractions.Helpers;
using Core.MediatR;
using Serilog;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        Log.ForContext("deploymentId", command.DeploymentId).Information("Adding log");
        var currentLog = await storageService.DownloadAsync(LogHelpers.GetLogFileName(command.DeploymentId), cancellationToken) ?? new MemoryStream();
        Log.ForContext("currentLog", currentLog.Length).Information("Got current log");

        var writer = new StreamWriter(currentLog);
        writer.WriteLine(LogHelpers.GetLogEntry(command.Log, command.Level));
        writer.Flush();
        currentLog.Position = 0;
        Log.ForContext("currentLog", currentLog.Length).Information("Uploading log");

        await storageService.UploadAsync(LogHelpers.GetLogFileName(command.DeploymentId), currentLog, cancellationToken);
        
        Log.ForContext("deploymentId", command.DeploymentId).Information("Uploaded log");

        return new AddLogResponse();
    }
}
