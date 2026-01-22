using Api.Abstractions;

namespace Api.Data;

public sealed record GetProjectEnvironmentResponse(
    bool AutoDeploy,
    ProjectType ProjectType,
    List<ProjectEnvironmentVariableDto> Variables);
