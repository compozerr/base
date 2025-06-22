using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.Extensions;
using Core.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe.Endpoints.Subscriptions.CancelSubscription;
using Stripe.Services;

namespace Api.Endpoints.Projects.Project.Delete;

public sealed class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IHostingApiFactory hostingApiFactory,
    ISender sender,
    IStripeService stripeService,
    IDeploymentRepository deploymentRepository) : ICommandHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    public async Task<DeleteProjectResponse> Handle(DeleteProjectCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ?? throw new InvalidOperationException("Should not be able to be here if null");

        if (project.ServerId is { } serverId)
        {
            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
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
            cancellationToken);

        return new();
    }

    private async Task CancelSubscriptionAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var loggerWithContext = Log.ForContext("ProjectId", projectId);
        loggerWithContext.Information("Cancelling subscription for project {ProjectId}.", projectId);

        var subscriptions = await stripeService.GetSubscriptionsForUserAsync(cancellationToken);

        if (subscriptions is null || !subscriptions.Any())
        {
            loggerWithContext.Error("No subscriptions found for user. Cannot cancel subscription for project {ProjectId}.", projectId);
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
