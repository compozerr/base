using Core.MediatR;

namespace Api.Hosting.Endpoints.VMPooling.InitiatePoolSync;

public sealed record InitiatePoolSyncCommand : ICommand<InitiatePoolSyncResponse>;
