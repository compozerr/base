using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
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
    ISubscriptionsService subscriptionsService) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
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

        var subscription = await subscriptionsService.CreateSubscriptionTierAsync(
            command.ProjectId,
            serverTier.Id,
            itemDescription: invoiceDescription,
            cancellationToken: cancellationToken);

        environment.StripeSubscriptionId = subscription.Id;
        environment.BillingStatus = EnvironmentBillingStatusExtensions.FromStripeStatus(subscription.Status);

        // Server allocation runs AFTER subscription creation (so Stripe compensation in the
        // try/catch below still rolls back billing if allocation throws) and BEFORE AddAsync.
        environment.ServerId = await ResolveServerIdAsync(command, project, cancellationToken);

        try
        {
            await environmentRepository.AddAsync(environment, cancellationToken);
        }
        catch
        {
            // Compensate: cancel the just-created Stripe subscription to avoid orphaned billing
            await subscriptionsService.CancelSubscriptionAsync(
                subscription.Id,
                cancelImmediately: true,
                CancellationToken.None);
            throw;
        }

        Log.ForContext(nameof(environment), environment, true)
           .Information("Created {Type} environment '{Name}' for project {ProjectId} with subscription {SubscriptionId} on server {ServerId}",
               command.Type, command.Name, command.ProjectId, subscription.Id, environment.ServerId);

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

            // Visibility filtering (AdminOnly etc.) is Spec A's job; this subtask only
            // verifies the server is live. Once Spec A lands, an IServerVisibilityFilter
            // check belongs here.
            return server.Id;
        }

        var allocation = await serverAllocator.AllocateForLocationAsync(project.LocationId, cancellationToken);
        return allocation.ServerId;
    }
}
