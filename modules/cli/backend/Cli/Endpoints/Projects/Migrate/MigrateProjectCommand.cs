using Core.MediatR;

namespace Cli.Endpoints.Projects.Migrate;

public sealed record MigrateProjectCommand(
    string Name,
    string RepoUrl,
    string LocationIso,
    string Tier,
    string? StartCommand,
    string? DockerComposeFile) : ICommand<MigrateProjectResponse>;
