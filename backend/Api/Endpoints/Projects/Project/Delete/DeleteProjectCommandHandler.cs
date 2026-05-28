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
    IProjectEnvironmentRepository environmentRepository,
    ISender sender,
    ISubscriptionsService subscriptionService,
    IDeploymentRepository deploymentRepository) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Should not be able to be here if null");

        await TearDownEnvironmentsAsync(project.Id, cancellationToken);

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

    /// <summary>
    /// F-P1-2: tear down each env's VM on the env's OWN server using the correct
    /// hosting call. Preview envs use a distinct VM identifier
    /// (<c>projectId--preview--environmentName</c>) and require
    /// <see cref="IHostingApi.DeletePreviewEnvironmentAsync"/>; the prior code
    /// called <see cref="IHostingApi.DeleteProjectAsync"/> for every distinct
    /// server, leaving preview VMs on non-prod servers alive while the DB row
    /// was deleted (orphaned compute).
    ///
    /// If ANY env's teardown fails we abort BEFORE the DB row is deleted (the
    /// caller never reaches <c>DeleteAsync</c>) and surface the failure, so the
    /// user can retry the delete once the underlying issue is fixed rather than
    /// silently leaking VMs.
    /// </summary>
    private async Task TearDownEnvironmentsAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var environments = await environmentRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var failures = new List<Exception>();
        foreach (var environment in environments)
        {
            try
            {
                // Resolve the env's own server. Null = never allocated (pre-backfill
                // transition row); there's no VM to tear down, so skip it.
                var server = await environmentTargetResolver.TryGetServerAsync(environment.Id, cancellationToken);
                if (server is null)
                    continue;

                var hostingApi = await environmentTargetResolver.GetHostingApiAsync(environment.Id, cancellationToken);

                if (environment.Type == Data.EnvironmentType.Preview && environment.Name is not null)
                {
                    await hostingApi.DeletePreviewEnvironmentAsync(projectId, environment.Name);
                }
                else
                {
                    await hostingApi.DeleteProjectAsync(projectId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to tear down environment {EnvironmentId} ({EnvironmentType}) for project {ProjectId}; aborting delete to avoid orphaned compute.",
                    environment.Id, environment.Type, projectId);
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
            throw new AggregateException(
                $"Failed to tear down {failures.Count} environment(s) for project {projectId}; the project was NOT deleted. Retry after resolving the hosting failure.",
                failures);
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
