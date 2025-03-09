using Api.Abstractions;

namespace Cli.Endpoints.Repos;

public sealed record CreateRepoResponse(
    string CloneUrl,
    string GitUrl,
    string RepoName,
    ProjectId ProjectId);
