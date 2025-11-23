using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.Abstractions;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class MakeSureParentDomainExists_ExternalDomainAddedEventHandler(
    IDomainRepository domainRepository,
    IProjectRepository projectRepository,
    IProjectServiceRepository projectServiceRepository,
    ISubdomainHashService subdomainHashService) : EntityDomainEventHandlerBase<ExternalDomainAddedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<MakeSureParentDomainExists_ExternalDomainAddedEventHandler>();
    protected override async Task HandleBeforeSaveAsync(ExternalDomainAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        var currentDomains = await domainRepository.GetFilteredAsync(
           x => x.ProjectId == domainEvent.Entity.ProjectId,
           cancellationToken);

        if (
            !currentDomains.Any(
                x =>
                x.Id != domainEvent.Entity.Id &&
                x.Port == domainEvent.Entity.Port &&
                x.Protocol == domainEvent.Entity.Protocol))
        {
            _logger.Information("No parent domain found for project {ProjectId} on port {Port} and protocol {Protocol}. Creating one.",
                domainEvent.Entity.ProjectId,
                domainEvent.Entity.Port,
                domainEvent.Entity.Protocol);

            var projectService = await projectServiceRepository.GetSingleAsync(
                x => x.Port == domainEvent.Entity.Port && x.Name == domainEvent.Entity.ServiceName && x.ProjectId == domainEvent.Entity.ProjectId,
                cancellationToken) ?? throw new InvalidOperationException("Project service not found");

            if (projectService.Protocol != domainEvent.Entity.Protocol)
            {
                projectService.Protocol = domainEvent.Entity.Protocol;
                await projectServiceRepository.UpdateAsync(
                    projectService,
                    cancellationToken);
            }

            var project = await projectRepository.GetByIdAsync(
                domainEvent.Entity.ProjectId,
                cancellationToken) ?? throw new InvalidOperationException("Project not found");

            var parentDomain = BuildCustomInternalDomain(
                project,
                projectService);

            await domainRepository.AddAsync(
                parentDomain,
                cancellationToken);
        }
        else
        {
            _logger.Information("Parent domain already exists for project {ProjectId} on port {Port} and protocol {Protocol}. No action needed.",
                domainEvent.Entity.ProjectId,
                domainEvent.Entity.Port,
                domainEvent.Entity.Protocol);
        }
    }

    private InternalDomain BuildCustomInternalDomain(Data.Project project, ProjectService service)
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
