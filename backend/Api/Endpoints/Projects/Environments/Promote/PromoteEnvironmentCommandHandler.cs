using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Promote;

public sealed class PromoteEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    IDeploymentRepository deploymentRepository,
    ApiDbContext context) : ICommandHandler<PromoteEnvironmentCommand, PromoteEnvironmentResponse>
{
    public async Task<PromoteEnvironmentResponse> Handle(
        PromoteEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var previewEnvironment = await environmentRepository.GetByIdWithProjectAsync(
            command.EnvironmentId, cancellationToken)
            ?? throw new ArgumentException($"Environment with ID {command.EnvironmentId} not found.");

        if (previewEnvironment.Type != EnvironmentType.Preview)
            throw new InvalidOperationException("Only preview environments can be promoted to production.");

        // Get the latest successful deployment from the preview environment
        var latestDeployment = await context.Deployments
            .Where(d => d.EnvironmentId == command.EnvironmentId
                     && d.Status == DeploymentStatus.Completed)
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No successful deployment found for this preview environment.");

        // Find or get the production environment
        var productionEnvironment = await context.Environments
            .FirstOrDefaultAsync(e => e.ProjectId == command.ProjectId
                                   && e.Type == EnvironmentType.Production,
                cancellationToken)
            ?? throw new InvalidOperationException("No production environment found for this project.");

        // Create a new deployment targeting the production environment with the same commit
        var newDeployment = new Deployment
        {
            Id = DeploymentId.CreateNew(),
            ProjectId = command.ProjectId,
            UserId = latestDeployment.UserId,
            CommitHash = latestDeployment.CommitHash,
            CommitMessage = latestDeployment.CommitMessage,
            CommitAuthor = latestDeployment.CommitAuthor,
            CommitBranch = latestDeployment.CommitBranch,
            CommitEmail = latestDeployment.CommitEmail,
            EnvironmentId = productionEnvironment.Id,
            Status = DeploymentStatus.Queued
        };

        var deployment = await deploymentRepository.AddAsync(newDeployment, cancellationToken);

        return new PromoteEnvironmentResponse(deployment.Id.Value.ToString());
    }
}
