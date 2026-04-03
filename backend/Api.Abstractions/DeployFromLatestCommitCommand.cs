using Core.MediatR;

namespace Api.Abstractions;

public sealed record DeployFromLatestCommitCommand(
	ProjectId ProjectId,
	ProjectEnvironmentId? EnvironmentId = null) : ICommand<DeployFromLatestCommitResponse>;
