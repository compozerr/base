using System.Collections.Concurrent;
using Api.Abstractions.Helpers;
using Core.MediatR;
using Serilog;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService
    ) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    // Per-deployment semaphores to prevent concurrent writes to the same log file
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var deploymentId = command.DeploymentId.ToString();

        // Get or create a semaphore for this specific deployment
        var semaphore = _semaphores.GetOrAdd(deploymentId, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
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
        finally
        {
            semaphore.Release();
        }
    }
}
