using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Repositories;
using Api.Data.Services;
using Cli.Endpoints.Projects;
using Core.MediatR;
using Serilog;
using Stripe.Services;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository,
    IServerRepository serverRepository,
    IServerAllocator serverAllocator,
    ISubscriptionsService subscriptionsService,
    IServerVisibilityFilter serverVisibilityFilter) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
{
    public async Task<CreateEnvironmentResponse> Handle(
        CreateEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var subdomain = command.Type == EnvironmentType.Preview
            ? SubdomainHelper.GenerateSubdomain(project.Name, command.Branch ?? command.Name)
            : null;

        var serverTier = ServerTiers.GetById(new ServerTierId(command.Tier));

        var environment = new Data.ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = command.ProjectId,
            Name = command.Name,
            Type = command.Type,
            Branches = command.Branch is not null ? [command.Branch] : [],
            AutoDeploy = command.Branch is not null,
            Subdomain = subdomain,
            ServerTierId = serverTier.Id
        };

        var invoiceDescription = Data.ProjectEnvironment.FormatInvoiceDescription(project.Name, command.Name, serverTier.Id);

        // F-P1-3: subscription creation, server allocation and AddAsync all live
        // inside ONE compensation scope. The previous code allocated the server
        // BEFORE the try/catch, so a throw from ResolveServerIdAsync left the
        // just-created Stripe subscription orphaned. Now any post-subscription
        // failure (allocation, visibility rejection, AddAsync) cancels billing.
        string? subscriptionId = null;
        try
        {
            var subscription = await subscriptionsService.CreateSubscriptionTierAsync(
                command.ProjectId,
                serverTier.Id,
                itemDescription: invoiceDescription,
                cancellationToken: cancellationToken);

            subscriptionId = subscription.Id;
            environment.StripeSubscriptionId = subscription.Id;
            environment.BillingStatus = EnvironmentBillingStatusExtensions.FromStripeStatus(subscription.Status);

            environment.ServerId = await ResolveServerIdAsync(command, project, cancellationToken);

            await environmentRepository.AddAsync(environment, cancellationToken);
        }
        catch
        {
            // Compensate: cancel the just-created Stripe subscription to avoid orphaned billing.
            if (subscriptionId is not null)
            {
                await subscriptionsService.CancelSubscriptionAsync(
                    subscriptionId,
                    cancelImmediately: true,
                    CancellationToken.None);
            }
            throw;
        }

        Log.ForContext(nameof(environment), environment, true)
           .Information("Created {Type} environment '{Name}' for project {ProjectId} with subscription {SubscriptionId} on server {ServerId}",
               command.Type, command.Name, command.ProjectId, subscriptionId, environment.ServerId);

        return new CreateEnvironmentResponse(
            environment.Id.Value,
            environment.Subdomain,
            serverTier.Id.Value,
            serverTier.Price.Value,
            environment.BillingStatus.ToString());
    }

    private async Task<ServerId> ResolveServerIdAsync(
        CreateEnvironmentCommand command,
        Data.Project project,
        CancellationToken cancellationToken)
    {
        if (command.ServerId is { } requestedServerId)
        {
            var server = await serverRepository.GetByIdAsync(requestedServerId, cancellationToken)
                ?? throw new InvalidOperationException($"Server {requestedServerId} not found.");

            // F-P1-4: an explicit ServerId bypasses the picker, so it MUST still
            // run through the same visibility filter — otherwise a non-admin can
            // hand-craft a request targeting an AdminOnly server. Reject with the
            // same controlled validation error the allocator uses, and don't leak
            // whether the server actually exists.
            var isVisible = serverVisibilityFilter
                .ApplyForCurrentUser(new[] { server })
                .Any();

            if (!isVisible)
                throw new NoAvailableServerException("Server not available for your account.");

            return server.Id;
        }

        var allocation = await serverAllocator.AllocateForLocationAsync(project.LocationId, cancellationToken);
        return allocation.ServerId;
    }
}
