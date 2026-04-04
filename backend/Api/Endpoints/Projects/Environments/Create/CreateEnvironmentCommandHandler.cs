using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Serilog;
using Stripe.Services;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository,
    ISubscriptionsService subscriptionsService) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
{
    public async Task<CreateEnvironmentResponse> Handle(
        CreateEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var subdomain = command.Type == EnvironmentType.Preview
            ? SubdomainHelper.GenerateSubdomain(project.Name, command.Branch)
            : null;

        var serverTier = ServerTiers.GetById(new ServerTierId(command.Tier));

        var environment = new Data.ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = command.ProjectId,
            Name = command.Name,
            Type = command.Type,
            Branches = [command.Branch],
            AutoDeploy = true,
            Subdomain = subdomain,
            ServerTierId = serverTier.Id
        };

        var invoiceDescription = $"{project.Name} — {command.Name} ({serverTier.Id.Value})";

        var subscription = await subscriptionsService.CreateSubscriptionTierAsync(
            command.ProjectId,
            serverTier.Id,
            itemDescription: invoiceDescription,
            cancellationToken: cancellationToken);

        environment.StripeSubscriptionId = subscription.Id;
        environment.BillingStatus = EnvironmentBillingStatusExtensions.FromStripeStatus(subscription.Status);

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
           .Information("Created {Type} environment '{Name}' for project {ProjectId} with subscription {SubscriptionId}",
               command.Type, command.Name, command.ProjectId, subscription.Id);

        return new CreateEnvironmentResponse(
            environment.Id.Value,
            environment.Subdomain,
            serverTier.Id.Value,
            serverTier.Price.Value,
            environment.BillingStatus.ToString());
    }

}
