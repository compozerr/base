using Core.MediatR;

namespace Api.Hosting.Endpoints.Deployments.Logs.Add;

public sealed class AddLogCommandHandler(
    ) : ICommandHandler<AddLogCommand, AddLogResponse>
{
    public async Task<AddLogResponse> Handle(AddLogCommand command, CancellationToken cancellationToken = default)
    {
        var deployment = await _deploymentRepository.GetByIdAsync(command.DeploymentId, cancellationToken);
        return new AddLogResponse();
    }
}
