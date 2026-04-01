using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Serilog;

namespace Api.Endpoints.Projects.Environments.Delete;

public sealed class DeleteEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository) : ICommandHandler<DeleteEnvironmentCommand, DeleteEnvironmentResponse>
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

        // TODO: Cancel Stripe subscription if StripeSubscriptionId is set

        await environmentRepository.DeleteAsync(environment.Id, cancellationToken);

        Log.Information("Deleted environment {EnvironmentId} for project {ProjectId}",
            command.EnvironmentId, command.ProjectId);

        return new DeleteEnvironmentResponse();
    }
}
