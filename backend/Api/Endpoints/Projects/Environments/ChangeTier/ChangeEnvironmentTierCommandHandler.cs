using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stripe.Services;

namespace Api.Endpoints.Projects.Environments.ChangeTier;

public sealed class ChangeEnvironmentTierCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository,
    IDeploymentRepository deploymentRepository,
    IMediator mediator,
    ISubscriptionsService subscriptionsService) : ICommandHandler<ChangeEnvironmentTierCommand, ChangeEnvironmentTierResponse>
{
    public async Task<ChangeEnvironmentTierResponse> Handle(
        ChangeEnvironmentTierCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(command.EnvironmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Environment {command.EnvironmentId} not found.");

        if (environment.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        var serverTier = ServerTiers.GetById(new ServerTierId(command.Tier));
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
        var invoiceDescription = Data.ProjectEnvironment.FormatInvoiceDescription(project?.Name, environment.Name, serverTier.Id);

        environment.ServerTierId = serverTier.Id;

        if (!string.IsNullOrEmpty(environment.StripeSubscriptionId))
        {
            var subscription = await subscriptionsService.UpdateSubscriptionTierAsync(
                environment.StripeSubscriptionId,
                environment.ProjectId,
                serverTier.Id,
                itemDescription: invoiceDescription,
                cancellationToken: cancellationToken);

            environment.BillingStatus = EnvironmentBillingStatusExtensions.FromStripeStatus(subscription.Status);

            Log.Information(
                "Updated Stripe subscription {SubscriptionId} to tier {Tier} for environment {EnvironmentId}",
                subscription.Id, command.Tier, command.EnvironmentId);
        }
        else
        {
            Log.Warning(
                "Environment {EnvironmentId} has no Stripe subscription. Creating one for tier {Tier}.",
                command.EnvironmentId, command.Tier);

            var subscription = await subscriptionsService.CreateSubscriptionTierAsync(
                environment.ProjectId,
                serverTier.Id,
                itemDescription: invoiceDescription,
                cancellationToken: cancellationToken);

            environment.StripeSubscriptionId = subscription.Id;
            environment.BillingStatus = EnvironmentBillingStatusExtensions.FromStripeStatus(subscription.Status);
        }

        await environmentRepository.UpdateAsync(environment, cancellationToken);

        Log.Information(
            "Changed tier for environment {EnvironmentId} to {Tier} on project {ProjectId}",
            command.EnvironmentId, command.Tier, command.ProjectId);

        await RedeployLastDeploymentAsync(environment, cancellationToken);

        return new ChangeEnvironmentTierResponse(serverTier.Id.Value, serverTier.Price.Value);
    }

    private async Task RedeployLastDeploymentAsync(
        Data.ProjectEnvironment environment,
        CancellationToken cancellationToken)
    {
        var currentDeployment = await deploymentRepository
            .Query()
            .Where(d => d.EnvironmentId == environment.Id && d.Status == DeploymentStatus.Completed)
            .OrderByDescending(d => d.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDeployment is null)
        {
            Log.Information(
                "No completed deployment found for environment {EnvironmentId}. No redeployment will occur.",
                environment.Id);
            return;
        }

        var newDeploymentCommand = new DeployProjectCommand(
            environment.ProjectId,
            currentDeployment.CommitHash,
            currentDeployment.CommitMessage,
            currentDeployment.CommitAuthor,
            currentDeployment.CommitBranch,
            currentDeployment.CommitEmail,
            EnvironmentId: environment.Id);

        await mediator.Send(newDeploymentCommand, cancellationToken);
    }
}
