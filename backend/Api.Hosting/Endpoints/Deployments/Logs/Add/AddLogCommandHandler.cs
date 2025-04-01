using Core.MediatR;
using Storage;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    IStorageService storageService) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var currentLog = await storageService.DownloadAsync(command.DeploymentId.ToString(), cancellationToken) ?? new MemoryStream();

        var writer = new StreamWriter(currentLog);
        writer.WriteLine(command.Log);
        writer.Flush();
        currentLog.Position = 0;

        await storageService.UploadAsync(command.DeploymentId.ToString(), currentLog, cancellationToken);

        return new AddLogResponse();
    }
}
