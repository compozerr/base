using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed record CreateEnvironmentCommand(
    ProjectId ProjectId,
    string Name,
    string Branch,
    string Tier,
    EnvironmentType Type = EnvironmentType.Preview) : ICommand<CreateEnvironmentResponse>;
