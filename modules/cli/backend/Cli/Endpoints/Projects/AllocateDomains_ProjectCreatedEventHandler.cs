using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services;
using Cli.Abstractions;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AllocateDomains_ProjectCreatedEventHandler(
    ISubdomainHashService subdomainHashService) : EntityDomainEventHandlerBase<ProjectCreatedEvent>
{
    protected override Task HandleBeforeSaveAsync(ProjectCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        domainEvent.Entity.Domains = [.. GetDomains(domainEvent.Entity)];

        return Task.CompletedTask;
    }

    private List<InternalDomain> GetDomains(Project project)
    {
        var hash = subdomainHashService.GetHash(project.Id);

        return [GetFrontendDomain(hash, project.Id), GetBackendDomain(hash, project.Id)];
    }

    private static InternalDomain GetFrontendDomain(string hash, ProjectId projectId)
        => new()
        {
            ProjectId = projectId,
            ServiceName = "Frontend",
            Port = "1234",
            Protocol = "http",
            IsPrimary = true,
            Subdomain = $"{hash}",
        };

    private static InternalDomain GetBackendDomain(string hash, ProjectId projectId)
        => new()
        {
            ProjectId = projectId,
            ServiceName = "Backend",
            Port = "1235",
            Protocol = "http",
            Subdomain = $"api.{hash}",
        };
}