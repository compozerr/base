using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Core.Services;
using Database.Extensions;

namespace Api.Endpoints.Projects.Deployments.DeployProject;

public sealed record DeployProjectCommandHandler(
    IDeploymentRepository DeploymentRepository,
    IProjectRepository ProjectRepository,
    IFrontendLocation FrontendLocation) : ICommandHandler<DeployProjectCommand, DeployProjectResponse>
{
    public async Task<DeployProjectResponse> Handle(
        DeployProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = (await ProjectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken))!;

        var newDeployment = new Deployment
        {
            Id = DeploymentId.CreateNew(),
            ProjectId = command.ProjectId,
            Project = project,
            CommitHash = command.CommitHash,
            CommitMessage = command.CommitMessage,
            CommitAuthor = command.CommitAuthor,
            CommitBranch = command.CommitBranch,
            CommitEmail = command.CommitEmail,
            UserId = project.UserId,
            Status = DeploymentStatus.Queued
        };

        newDeployment.QueueDomainEvent<DeploymentQueuedEvent>();

        Log.ForContext(nameof(newDeployment), newDeployment)
           .Information("Deployment queued");

        var deployment = await DeploymentRepository.AddAsync(newDeployment, cancellationToken);

        Log.ForContext(nameof(newDeployment), newDeployment)
           .Information("Deployment domain events executed and sending response");

        return new DeployProjectResponse(GetStatusUrl(deployment.ProjectId, deployment.Id));
    }

    private string GetStatusUrl(ProjectId projectId, DeploymentId deploymentId)
        => FrontendLocation.GetFromPath($"projects/{projectId.Value}/deployments/{deploymentId.Value}").ToString();
}