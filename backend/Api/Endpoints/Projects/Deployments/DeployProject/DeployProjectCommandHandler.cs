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
    IProjectEnvironmentRepository ProjectEnvironmentRepository,
    IFrontendLocation FrontendLocation) : ICommandHandler<DeployProjectCommand, DeployProjectResponse>
{
    public async Task<DeployProjectResponse> Handle(
        DeployProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = (await ProjectRepository.GetByIdAsync(
            command.ProjectId,
            cancellationToken))!;

        var environmentId = command.EnvironmentId;

        if (environmentId is null)
        {
            var environment = await ProjectEnvironmentRepository.GetProjectEnvironmentByBranchAsync(
                command.ProjectId, command.CommitBranch);

            if (environment is null)
            {
                throw new InvalidOperationException(
                    $"No environment found for branch '{command.CommitBranch}' on project {command.ProjectId}. " +
                    "Cannot deploy without a target environment.");
            }

            environmentId = environment.Id;

            Log.Information(
                "Resolved EnvironmentId {EnvironmentId} from branch {Branch} for project {ProjectId}",
                environmentId, command.CommitBranch, command.ProjectId);
        }

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
            Status = DeploymentStatus.Queued,
            EnvironmentId = environmentId
        };

        newDeployment.QueueDomainEvent<DeploymentQueuedEvent>();

        Log.ForContext(nameof(newDeployment), newDeployment)
           .Information("Deployment queued");

        var deployment = await DeploymentRepository.AddAsync(
            newDeployment,
            cancellationToken);

        Log.ForContext(nameof(newDeployment), newDeployment)
           .Information("Deployment domain events executed and sending response");

        return new DeployProjectResponse(GetStatusUrl(deployment.ProjectId, deployment.Id));
    }

    private string GetStatusUrl(ProjectId projectId, DeploymentId deploymentId)
        => FrontendLocation.GetFromPath($"projects/{projectId.Value}/deployments/{deploymentId.Value}").ToString();
}
