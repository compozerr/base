using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment.ChangeAutoDeploy;

public sealed record ChangeAutoDeployCommand(
	ProjectId ProjectId,
	bool AutoDeploy) : ICommand<ChangeAutoDeployResponse>;
