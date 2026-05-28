using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.ChangeEnvironment;

public sealed class ChangeDomainEnvironmentCommandHandler(
    IDomainRepository domainRepository,
    ApiDbContext context) : ICommandHandler<ChangeDomainEnvironmentCommand, ChangeDomainEnvironmentResponse>
{
    public async Task<ChangeDomainEnvironmentResponse> Handle(
        ChangeDomainEnvironmentCommand command, CancellationToken cancellationToken = default)
    {
        var domain = await domainRepository.GetByIdAsync(command.DomainId, cancellationToken)
            ?? throw new ArgumentException($"Domain not found");

        if (domain is not ExternalDomain)
            throw new InvalidOperationException("Cannot change environment routing for internal domains");

        if (domain.ProjectId != command.ProjectId)
            throw new InvalidOperationException("Domain does not belong to this project");

        // F-P1-1: the target env must belong to the same project as the domain,
        // otherwise the resolver would route this domain to another project's
        // server (cross-project hijack). Reject (400) on mismatch.
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

        domain.ProjectEnvironmentId = command.EnvironmentId;
        domain.QueueDomainEvent<DomainChangeEvent>();

        await domainRepository.UpdateAsync(domain, cancellationToken);
        return new();
    }
}
