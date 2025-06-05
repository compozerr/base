using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Project.ChangeTier;

public sealed record ChangeTierCommand(
	ProjectId ProjectId,
	string Tier) : ICommand<ChangeTierResponse>;
