using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;
using Core.Services;
using Database.Extensions;

namespace Cli.Endpoints.Projects.Deployments;

public sealed record DeployProjectCommandHandler(
    IDeploymentRepository DeploymentRepository,
    ICurrentUserAccessor CurrentUserAccessor,
    IProjectRepository ProjectRepository,
    IFrontendLocation FrontendLocation) : ICommandHandler<DeployProjectCommand, DeployProjectResponse>
{
    public async Task<DeployProjectResponse> Handle(DeployProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var currentUserDeployments = await DeploymentRepository.GetDeploymentsForUserAsync(userId);

        var projectDeployments = currentUserDeployments.Where(d => d.ProjectId == command.ProjectId && d.Status != DeploymentStatus.Completed)
                                                       .ToList();

        var alreadyDeployingProject = projectDeployments.SingleOrDefault(
            x => x.Status == DeploymentStatus.Deploying && x.ProjectId == command.ProjectId && x.CommitHash == command.CommitHash);

        if (alreadyDeployingProject is { Id: { } deploymentId })
        {
            Log.ForContext(nameof(command), command, true)
               .Information("Already deploying");

            var frontendPath = GetStatusUrl(command.ProjectId, deploymentId);

            return new DeployProjectResponse(frontendPath.ToString());
        }

        var project = await ProjectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken);

        var newDeployment = new Deployment
        {
            ProjectId = command.ProjectId,
            Project = project,
            CommitHash = command.CommitHash,
            UserId = userId,
            Status = DeploymentStatus.Queued
        };

        newDeployment.QueueDomainEvent<DeploymentQueuedEvent>();

        var deployment = await DeploymentRepository.AddAsync(newDeployment, cancellationToken);

        return new DeployProjectResponse(GetStatusUrl(deployment.ProjectId, deployment.Id));
    }

    private string GetStatusUrl(ProjectId projectId, DeploymentId deploymentId)
        => FrontendLocation.GetFromPath($"projects/{projectId.Value}/deployments/${deploymentId.Value}").ToString();
}