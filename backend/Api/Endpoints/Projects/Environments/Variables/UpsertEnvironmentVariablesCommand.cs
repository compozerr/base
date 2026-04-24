using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Variables;

public sealed record UpsertEnvironmentVariablesCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId,
    List<ProjectEnvironmentVariableDto> Variables) : ICommand<UpsertEnvironmentVariablesResponse>;

public sealed record UpsertEnvironmentVariablesResponse;
