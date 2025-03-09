using Core.MediatR;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommand(
    string RepoName,
    string RepoUrl) : ICommand<CreateProjectResponse>;