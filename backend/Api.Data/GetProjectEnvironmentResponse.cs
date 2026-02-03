using Api.Abstractions;

namespace Api.Data;

public sealed record GetProjectEnvironmentResponse(
    bool AutoDeploy,
    ProjectType ProjectType,
    ProjectTypeConfigurationDto Configuration,
    List<ProjectEnvironmentVariableDto> Variables);
