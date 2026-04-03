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

        environment.ServerTierId = serverTier.Id;

        if (!string.IsNullOrEmpty(environment.StripeSubscriptionId))
        {
            var subscription = await subscriptionsService.UpdateSubscriptionTierAsync(
                environment.StripeSubscriptionId,
                environment.ProjectId,
                serverTier.Id,
                cancellationToken: cancellationToken);

            environment.BillingStatus = MapBillingStatus(subscription.Status);

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
                cancellationToken: cancellationToken);

            environment.StripeSubscriptionId = subscription.Id;
            environment.BillingStatus = MapBillingStatus(subscription.Status);
        }

        await environmentRepository.UpdateAsync(environment, cancellationToken);

        Log.Information(
            "Changed tier for environment {EnvironmentId} to {Tier} on project {ProjectId}",
            command.EnvironmentId, command.Tier, command.ProjectId);

        await RedeployLastDeploymentAsync(environment, cancellationToken);

        return new ChangeEnvironmentTierResponse(serverTier.Id.Value, serverTier.Price.Value);
    }

    private static EnvironmentBillingStatus MapBillingStatus(string stripeStatus) => stripeStatus switch
    {
        "trialing" => EnvironmentBillingStatus.Trialing,
        "active" => EnvironmentBillingStatus.Active,
        "past_due" or "unpaid" => EnvironmentBillingStatus.Suspended,
        "canceled" or "incomplete_expired" => EnvironmentBillingStatus.Cancelled,
        _ => EnvironmentBillingStatus.Active,
    };

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
