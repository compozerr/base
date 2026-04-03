using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.MediatR;
using Serilog;
using Stripe.Services;

namespace Api.Endpoints.Projects.Environments.Delete;

public sealed class DeleteEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository,
    ISubscriptionsService subscriptionsService,
    IHostingApiFactory hostingApiFactory) : ICommandHandler<DeleteEnvironmentCommand, DeleteEnvironmentResponse>
{
    public async Task<DeleteEnvironmentResponse> Handle(
        DeleteEnvironmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await environmentRepository.GetByIdAsync(command.EnvironmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Environment {command.EnvironmentId} not found.");

        if (environment.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        if (environment.Type == EnvironmentType.Production)
            throw new InvalidOperationException("Cannot delete production environment.");

        if (!string.IsNullOrEmpty(environment.StripeSubscriptionId))
        {
            await subscriptionsService.CancelSubscriptionAsync(
                environment.StripeSubscriptionId,
                cancelImmediately: true,
                cancellationToken);

            Log.Information("Cancelled Stripe subscription {SubscriptionId} for environment {EnvironmentId}",
                environment.StripeSubscriptionId, command.EnvironmentId);
        }

        // Delete preview VM on hosting server
        if (environment.Name is not null)
        {
            var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
            if (project?.ServerId is { } serverId)
            {
                var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
                await hostingApi.DeletePreviewEnvironmentAsync(command.ProjectId, environment.Name);

                Log.Information("Deleted preview VM for environment {EnvironmentName} on project {ProjectId}",
                    environment.Name, command.ProjectId);
            }
        }

        await environmentRepository.DeleteAsync(environment.Id, cancellationToken);

        Log.Information("Deleted environment {EnvironmentId} for project {ProjectId}",
            command.EnvironmentId, command.ProjectId);

        return new DeleteEnvironmentResponse();
    }
}
