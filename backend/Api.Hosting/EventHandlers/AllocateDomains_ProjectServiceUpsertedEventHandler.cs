using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.Services;
using Api.Hosting.Services;
using Core.Abstractions;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.EventHandlers;

public sealed class AllocateDomains_ProjectServiceUpsertedEventHandler(
    IProjectRepository projectRepository,
    ISubdomainHashService subdomainHashService,
    ApiDbContext context) : EntityDomainEventHandlerBase<ProjectServiceUpsertedEvent>
{
    protected override async Task HandleBeforeSaveAsync(ProjectServiceUpsertedEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.Entity.Protocol == Protocol.Tcp)
            return;

        var project = await projectRepository.GetProjectByIdWithDomainsAsync(
            domainEvent.Entity.ProjectId) ?? throw new ArgumentException("Project not found");

        ProjectEnvironment? environment = null;
        if (domainEvent.Entity.ProjectEnvironmentId is { } envId)
        {
            environment = await context.Set<ProjectEnvironment>()
                .FirstOrDefaultAsync(e => e.Id == envId, cancellationToken);
        }

        project.Domains ??= [];

        var existingDomain = project.Domains
            .OfType<InternalDomain>()
            .FirstOrDefault(d =>
                d.ServiceName == domainEvent.Entity.Name
                && d.Port == domainEvent.Entity.Port
                && d.ProjectEnvironmentId == domainEvent.Entity.ProjectEnvironmentId);

        var newDomain = BuildInternalDomain(project, domainEvent.Entity, environment);

        if (existingDomain != null)
        {
            if (existingDomain.Subdomain != newDomain.Subdomain)
            {
                existingDomain.Subdomain = newDomain.Subdomain;
                existingDomain.QueueDomainEvent<DomainChangeEvent>();
            }
        }
        else
        {
            newDomain.QueueDomainEvent<DomainChangeEvent>();
            project.Domains.Add(newDomain);
        }
    }

    private InternalDomain BuildInternalDomain(Project project, ProjectService service, ProjectEnvironment? environment)
    {
        string subdomain;

        if (environment?.Type == EnvironmentType.Preview && environment.Subdomain is not null)
        {
            // Always prefix with service name — matches hosting convention
            subdomain = $"{service.Name.ToLower()}-{environment.Subdomain}.preview";
        }
        else
        {
            // Production: existing pattern
            var hash = subdomainHashService.GetHash(project.Id);
            subdomain = $"{service.Name.ToLower()}.{hash}";
        }

        return new()
        {
            ProjectId = project.Id,
            ProjectEnvironmentId = service.ProjectEnvironmentId,
            ServiceName = service.Name,
            Port = service.Port,
            Protocol = service.Protocol,
            IsPrimary = false,
            Subdomain = subdomain,
        };
    }
}
