using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.Services;
using Api.Hosting.Services;
using Core.Abstractions;
using Database.Extensions;

namespace Api.Hosting.EventHandlers;

public sealed class AllocateDomains_ProjectServiceUpsertedEventHandler(
    IProjectRepository projectRepository,
    ISubdomainHashService subdomainHashService) : EntityDomainEventHandlerBase<ProjectServiceUpsertedEvent>
{
    protected override async Task HandleBeforeSaveAsync(ProjectServiceUpsertedEvent domainEvent, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(
            domainEvent.Entity.ProjectId,
            cancellationToken) ?? throw new ArgumentException("Project not found");

        project.Domains ??= [];
        var existingDomain = project.Domains.FirstOrDefault(d => d.ServiceName == domainEvent.Entity.Name && d.Port == domainEvent.Entity.Port);
        var newDomain = BuildCustomInternalDomain(project, domainEvent.Entity);

        if (existingDomain == null || !existingDomain.GetValue.Contains(newDomain.Subdomain))
        {
            newDomain.QueueDomainEvent<DomainChangeEvent>();
            project.Domains.Add(newDomain);
            await projectRepository.UpdateAsync(project, cancellationToken);
        }
    }

    private InternalDomain BuildCustomInternalDomain(Project project, ProjectService service)
    {
        var hash = subdomainHashService.GetHash(project.Id);

        return new()
        {
            ProjectId = project.Id,
            ServiceName = service.Name,
            Port = service.Port,
            Protocol = service.Protocol,
            IsPrimary = false,
            Subdomain = $"{service.Name.ToLower()}.{hash}",
        };
    }
}
