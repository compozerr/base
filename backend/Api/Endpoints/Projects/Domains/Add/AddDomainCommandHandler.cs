using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class AddDomainCommandHandler(
    IDomainRepository domainRepository,
    ApiDbContext context) : ICommandHandler<AddDomainCommand, AddDomainResponse>
{
    public async Task<AddDomainResponse> Handle(
        AddDomainCommand command,
        CancellationToken cancellationToken = default)
    {
        var service = await context.ProjectServices
            .FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId && s.Name == command.ServiceName, cancellationToken);

        if (service is null)
            throw new InvalidOperationException($"Service '{command.ServiceName}' not found for project");

        // F-P1-1: an env-scoped domain must point at an env owned by the SAME
        // project, otherwise the resolver would route this project's domain to
        // another project's server (cross-project hijack). Reject (400) on
        // mismatch rather than persisting a poisoned ProjectEnvironmentId.
        if (command.EnvironmentId is { } environmentId)
        {
            var envProjectId = await context.ProjectEnvironments
                .Where(e => e.Id == environmentId)
                .Select(e => e.ProjectId)
                .FirstOrDefaultAsync(cancellationToken);

            if (envProjectId is null)
                throw new ArgumentException($"Environment '{environmentId}' not found.");

            if (envProjectId != command.ProjectId)
                throw new ArgumentException("Environment does not belong to this project.");
        }

        var externalDomainEntity = new ExternalDomain
        {
            ProjectId = command.ProjectId,
            ProjectEnvironmentId = command.EnvironmentId,
            ServiceName = command.ServiceName,
            Port = service.Port,
            Protocol = command.Protocol,
            Value = command.Domain,
            IsVerified = false,
        };

        externalDomainEntity.QueueDomainEvent<ExternalDomainAddedEvent>();

        var domain = await domainRepository.AddAsync(externalDomainEntity, cancellationToken);

        return new(domain.Id.Value.ToString());
    }
}
