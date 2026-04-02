using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Serilog;

namespace Api.Endpoints.Projects.Environments.Create;

public sealed class CreateEnvironmentCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IProjectRepository projectRepository) : ICommandHandler<CreateEnvironmentCommand, CreateEnvironmentResponse>
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

        // TODO: Create Stripe subscription via SubscriptionService

        await environmentRepository.AddAsync(environment, cancellationToken);

        Log.ForContext(nameof(environment), environment, true)
           .Information("Created {Type} environment '{Name}' for project {ProjectId}",
               command.Type, command.Name, command.ProjectId);

        return new CreateEnvironmentResponse(
            environment.Id.Value,
            environment.Subdomain,
            serverTier.Id.Value,
            serverTier.Price.Value);
    }
}
