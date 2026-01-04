using Api.Abstractions;

namespace Cli.Endpoints.Projects.Migrate;

public sealed record MigrateProjectResponse(
    ProjectId ProjectId,
    CompozerrConfig GeneratedConfig);

public sealed record CompozerrConfig(
    string Type,
    string Name,
    Guid Id,
    string? Start,
    string? DockerComposeFile);
