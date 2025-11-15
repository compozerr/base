using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed record DeployFromLatestCommitCommand(
	ProjectId ProjectId) : ICommand<DeployFromLatestCommitResponse>;
