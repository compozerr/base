using Api.Data;
using Core.MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public sealed record UpsertProjectEnvironmentVariablesCommand(
    Guid ProjectId,
    string Branch,
    List<ProjectEnvironmentVariableDto> Variables) : ICommand<UpsertProjectEnvironmentVariablesResponse>;

public sealed record UpsertProjectEnvironmentVariablesResponse;