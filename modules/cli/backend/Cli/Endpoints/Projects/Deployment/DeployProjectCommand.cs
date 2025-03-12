using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects.Deployment;

public sealed record DeployProjectCommand(
    ProjectId ProjectId,
    string CommitHash) : ICommand<DeployProjectResponse>;