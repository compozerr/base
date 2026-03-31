using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Deployments.DeployProject;
using Core.MediatR;
using Core.Services;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Promote;

public sealed class PromoteEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IDeploymentRepository deploymentRepository,
    IProjectRepository projectRepository,
    IFrontendLocation frontendLocation) : ICommandHandler<PromoteEnvironmentCommand, PromoteEnvironmentResponse>
{
    public async Task<PromoteEnvironmentResponse> Handle(
        PromoteEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(command.EnvironmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Environment {command.EnvironmentId} not found.");

        if (environment.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        if (environment.Type != EnvironmentType.Preview)
            throw new InvalidOperationException("Only preview environments can be promoted.");

        var latestDeployment = await deploymentRepository
            .Query()
            .Where(d => d.EnvironmentId == command.EnvironmentId && d.Status == DeploymentStatus.Completed)
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No completed deployment found for this environment.");

        var productionEnvironment = await environmentRepository
            .GetSingleAsync(e => e.ProjectId == command.ProjectId && e.Type == EnvironmentType.Production, cancellationToken)
            ?? throw new InvalidOperationException("No production environment found for this project.");

        var project = (await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken))!;

        var newDeployment = new Deployment
        {
            Id = DeploymentId.CreateNew(),
            ProjectId = command.ProjectId,
            Project = project,
            CommitHash = latestDeployment.CommitHash,
            CommitMessage = latestDeployment.CommitMessage,
            CommitAuthor = latestDeployment.CommitAuthor,
            CommitBranch = latestDeployment.CommitBranch,
            CommitEmail = latestDeployment.CommitEmail,
            UserId = project.UserId,
            Status = DeploymentStatus.Queued,
            EnvironmentId = productionEnvironment.Id
        };

        newDeployment.QueueDomainEvent<DeploymentQueuedEvent>();

        Log.ForContext(nameof(newDeployment), newDeployment)
           .Information("Promotion deployment queued from environment {PreviewEnvironmentId} to production",
               command.EnvironmentId);

        var deployment = await deploymentRepository.AddAsync(newDeployment, cancellationToken);

        var statusUrl = frontendLocation.GetFromPath(
            $"projects/{deployment.ProjectId.Value}/deployments/{deployment.Id.Value}").ToString();

        return new PromoteEnvironmentResponse(statusUrl);
    }
}
