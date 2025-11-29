using Api.Hosting.VMPooling.Jobs;
using Core.MediatR;

namespace Api.Hosting.Endpoints.VMPooling.InitiatePoolSync;

public sealed class InitiatePoolSyncCommandHandler : ICommandHandler<InitiatePoolSyncCommand, InitiatePoolSyncResponse>
{
	public async Task<InitiatePoolSyncResponse> Handle(InitiatePoolSyncCommand command, CancellationToken cancellationToken = default)
	{
		VMPoolSyncJob.Enqueue();
		return new InitiatePoolSyncResponse();
	}
}
