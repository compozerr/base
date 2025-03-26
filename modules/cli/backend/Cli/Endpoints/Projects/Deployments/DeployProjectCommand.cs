using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects.Deployments;

public sealed record DeployProjectCommand(
    ProjectId ProjectId,
    string CommitHash,
    string CommitMessage,
    string CommitAuthor,
    string CommitBranch,
    string CommitEmail) : ICommand<DeployProjectResponse>;