using Api.Abstractions;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.ProjectState;

public sealed record ProjectStateCommand(
	ProjectId ProjectId,
	Data.ProjectState State) : ICommand<ProjectStateResponse>;
