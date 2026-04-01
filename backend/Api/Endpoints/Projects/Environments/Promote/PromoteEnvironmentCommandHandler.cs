using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Promote;

public sealed class PromoteEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IDeploymentRepository deploymentRepository,
    IMediator mediator) : ICommandHandler<PromoteEnvironmentCommand, PromoteEnvironmentResponse>
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

        Log.Information("Promotion deployment queued from environment {PreviewEnvironmentId} to production",
            command.EnvironmentId);

        var response = await mediator.Send(new DeployProjectCommand(
            environment.ProjectId,
            latestDeployment.CommitHash,
            latestDeployment.CommitMessage,
            latestDeployment.CommitAuthor,
            latestDeployment.CommitBranch,
            latestDeployment.CommitEmail,
            EnvironmentId: productionEnvironment.Id
        ), cancellationToken);

        return new PromoteEnvironmentResponse(response.StatusUrl);
    }
}
