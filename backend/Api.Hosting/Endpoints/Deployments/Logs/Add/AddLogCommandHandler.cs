using System.Runtime.Caching;
using Api.Abstractions.Helpers;
using Core.MediatR;
using Serilog;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService
    ) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    // Per-deployment semaphores with automatic expiration to prevent memory leaks
    private static readonly MemoryCache _semaphoreCache = new("DeploymentSemaphores");
    private static readonly CacheItemPolicy _cachePolicy = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    private static SemaphoreSlim GetOrCreateSemaphore(string deploymentId)
    {
        var semaphore = _semaphoreCache.Get(deploymentId) as SemaphoreSlim;
        if (semaphore is null)
        {
            semaphore = new SemaphoreSlim(1, 1);
            _semaphoreCache.Set(deploymentId, semaphore, _cachePolicy);
        }
        return semaphore;
    }

    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var deploymentId = command.DeploymentId.ToString();

        // Get or create a semaphore for this specific deployment (auto-expires after 10 min inactivity)
        var semaphore = GetOrCreateSemaphore(deploymentId);

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            Log.ForContext("deploymentId", command.DeploymentId).Information("Adding log");

            var downloadedLog = await storageService.DownloadAsync(LogHelpers.GetLogFileName(command.DeploymentId), cancellationToken);

            await using var currentLog = new MemoryStream();

            // Copy existing content if available
            if (downloadedLog is not null)
            {
                // Ensure we copy from the beginning of the downloaded stream
                if (downloadedLog.CanSeek)
                {
                    downloadedLog.Position = 0;
                }
                await downloadedLog.CopyToAsync(currentLog, cancellationToken);
                await downloadedLog.DisposeAsync();
            }

            Log.ForContext("currentLog", currentLog.Length).Information("Got current log");

            // Position at end to append
            currentLog.Position = currentLog.Length;

            await using (var writer = new StreamWriter(currentLog, leaveOpen: true))
            {
                await writer.WriteLineAsync(LogHelpers.GetLogEntry(command.Log, command.Level));
                await writer.FlushAsync(cancellationToken);
            }

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
