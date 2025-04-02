using Api.Abstractions.Helpers;
using Core.MediatR;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var currentLog = await storageService.DownloadAsync(LogHelpers.GetLogFileName(command.DeploymentId), cancellationToken) ?? new MemoryStream();

        var writer = new StreamWriter(currentLog);
        writer.WriteLine(LogHelpers.GetLogEntry(command.Log, command.Level));
        writer.Flush();
        currentLog.Position = 0;

        await storageService.UploadAsync(LogHelpers.GetLogFileName(command.DeploymentId), currentLog, cancellationToken);

        return new AddLogResponse();
    }
}
