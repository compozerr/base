using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommand(
    string RepoName,
    string RepoUrl,
    LocationId LocationId) : ICommand<CreateProjectResponse>;