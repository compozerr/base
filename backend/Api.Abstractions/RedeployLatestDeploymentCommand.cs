using Core.MediatR;

namespace Api.Abstractions;

public sealed record RedeployLatestDeploymentCommand(
	ProjectId ProjectId) : ICommand<RedeployLatestDeploymentResponse>;
