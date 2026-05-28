using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Auth.Abstractions;
using Core.Extensions;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;
using Stripe.Endpoints.Subscriptions.CancelSubscription;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Services;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IEnvironmentTargetResolver environmentTargetResolver,
    ISender sender,
    ISubscriptionsService subscriptionService,
    IDeploymentRepository deploymentRepository) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Should not be able to be here if null");

        // Tear down on every server hosting an env of this project. Servers
        // that no longer host any env (e.g. orphaned legacy hint) are still
        // included by the resolver so the cleanup path doesn't leak VMs.
        var hostingApis = await environmentTargetResolver
            .GetDistinctHostingApisForProjectAsync(project.Id, cancellationToken);

        foreach (var hostingApi in hostingApis)
        {
            await hostingApi.DeleteProjectAsync(project.Id);
        }

        var deployingDeployments = await deploymentRepository.GetDeploymentsForProject(project.Id)
            .Where(d => d.Status == Data.DeploymentStatus.Queued || d.Status == Data.DeploymentStatus.Deploying)
            .ToListAsync(cancellationToken);

        if (deployingDeployments is { })
        {
            deployingDeployments.Apply(d => d.Status = Data.DeploymentStatus.Cancelled);
            await deploymentRepository.UpdateRangeAsync(deployingDeployments, cancellationToken);
        }

        await projectRepository.DeleteAsync(project.Id, cancellationToken);

        await CancelSubscriptionAsync(
            project.Id,
            project.OrganizationId,
            command.SkipOwnerCheck,
            cancellationToken);

        return new();
    }

    private async Task CancelSubscriptionAsync(
        ProjectId projectId,
        OrganizationId? organizationId,
        bool skipOwnerCheck = false,
        CancellationToken cancellationToken = default)
    {
        var loggerWithContext = Log.ForContext("ProjectId", projectId);
        loggerWithContext.Information("Cancelling subscription for project {ProjectId}.", projectId);

        List<SubscriptionDto> subscriptions;
        if (skipOwnerCheck && organizationId is not null)
        {
            subscriptions = await subscriptionService.GetSubscriptionsForOrganizationAsync(
                organizationId, cancellationToken);
        }
        else
        {
            if (skipOwnerCheck && organizationId is null)
            {
                loggerWithContext.Warning(
                    "SkipOwnerCheck is set but project {ProjectId} has no OrganizationId; falling back to current-user subscription lookup.",
                    projectId);
            }
            subscriptions = await subscriptionService.GetSubscriptionsForUserAsync(cancellationToken);
        }

        if (subscriptions is null || !subscriptions.Any())
        {
            loggerWithContext.Error("No subscriptions found for project's organization. Cannot cancel subscription for project {ProjectId}.", projectId);
            return; // No subscriptions to cancel
        }

        var subscription = subscriptions
            .FirstOrDefault(s => (s.Status == "active" || s.Status == "trialing") && s.ProjectId == projectId);

        if (subscription is { Id: not null })
        {
            await sender.Send(
                new CancelSubscriptionCommand(
                    subscription.Id,
                    CancelImmediately: true),
                cancellationToken);
        }
        else
        {
            loggerWithContext.Error("No active or trialing subscription found for project {ProjectId}. Cannot cancel subscription.", projectId);
        }
    }
}
