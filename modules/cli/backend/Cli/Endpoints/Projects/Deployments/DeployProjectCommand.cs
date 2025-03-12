using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Projects.Deployments;

public sealed record DeployProjectCommand(
    ProjectId ProjectId,
    string CommitHash) : ICommand<DeployProjectResponse>;