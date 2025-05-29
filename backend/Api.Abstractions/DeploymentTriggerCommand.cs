using Core.MediatR;

namespace Api.Abstractions;

public sealed record DeploymentTriggerCommand(
	ProjectId ProjectId) : ICommand<DeploymentTriggerResponse>;
