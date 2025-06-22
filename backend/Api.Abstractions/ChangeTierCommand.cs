using Core.MediatR;

namespace Api.Abstractions;

public sealed record ChangeTierCommand(
	ProjectId ProjectId,
	string Tier) : ICommand<ChangeTierResponse>;
