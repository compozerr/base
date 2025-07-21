namespace Api.Data;

public sealed record GetProjectEnvironmentResponse(
    bool AutoDeploy,
    List<ProjectEnvironmentVariableDto> Variables);
