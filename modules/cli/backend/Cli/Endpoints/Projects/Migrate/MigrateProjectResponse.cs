using Api.Abstractions;

namespace Cli.Endpoints.Projects.Migrate;

public sealed record MigrateProjectResponse(
    bool Success,
    ProjectId? ProjectId,
    CompozerrConfig? GeneratedConfig,
    MigrateProjectError? Error)
{
    public static MigrateProjectResponse Ok(ProjectId projectId, CompozerrConfig config) =>
        new(true, projectId, config, null);

    public static MigrateProjectResponse Fail(MigrateProjectError error) =>
        new(false, null, null, error);
}

public sealed record MigrateProjectError(
    string Code,
    string Message,
    string? SettingsUrl);

public sealed record CompozerrConfig(
    string Type,
    string Name,
    Guid Id,
    string? Start,
    string? DockerComposeFile);
