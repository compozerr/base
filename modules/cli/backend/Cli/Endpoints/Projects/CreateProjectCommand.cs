using Core.MediatR;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommand(
    string RepoName,
    string RepoUrl,
    string LocationIso,
    string Tier) : ICommand<CreateProjectResponse>;