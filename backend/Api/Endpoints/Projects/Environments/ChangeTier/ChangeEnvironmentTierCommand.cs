using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.ChangeTier;

public sealed record ChangeEnvironmentTierCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId,
    string Tier) : ICommand<ChangeEnvironmentTierResponse>;
