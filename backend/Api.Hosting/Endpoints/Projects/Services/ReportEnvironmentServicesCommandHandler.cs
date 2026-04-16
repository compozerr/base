using Api.Data;
using Api.Data.Repositories;
using Core.Extensions;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed class ReportEnvironmentServicesCommandHandler(
    IProjectServiceRepository projectServiceRepository,
    IProjectRepository projectRepository,
    IDomainRepository domainRepository)
    : ICommandHandler<ReportEnvironmentServicesCommand, ReportServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<ReportServicesResponse> Handle(
        ReportEnvironmentServicesCommand command,
        CancellationToken cancellationToken)
    {
        // Services and domains are scoped to a single environment. The unique
        // index on (ProjectId, ProjectEnvironmentId, Name) means the upsert-by-name
        // lookup is safe to scope to this env: a name match in another env is a
        // different row altogether and must not be touched from here. Soft-deleted
        // domains are filtered out so the system-domain cleanup below doesn't try
        // to re-delete them (which throws).
        var project = await projectRepository.GetSingleAsync(
            x => x.Id == command.ProjectId,
            i => i.Include(x => x.ProjectServices!.Where(s => s.ProjectEnvironmentId == command.EnvironmentId))
                    .Include(x => x.Domains!.Where(d => d.DeletedAtUtc == null
                                                     && d.ProjectEnvironmentId == command.EnvironmentId)), cancellationToken)
                    ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? [];

        // Port-based rename detection operates within the reporting env. GroupBy
        // guards against two services in the same env accidentally sharing a port.
        var portToServiceName = project.ProjectServices?
            .GroupBy(s => s.Port)
            .ToDictionary(g => g.Key, g => g.First().Name) ?? [];

        foreach (var serviceInfo in command.Services)
        {
            ProjectService serviceToProcess;
            bool isNewService = false;
            bool serviceChanged = true;

            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
                // Resurrect previously soft-deleted rows. Without this, a service that
                // briefly disappeared from a single port-detection run (and got tombstoned
                // by the stale-IsSystem cleanup below) stays hidden from the UI forever,
                // because the name+port match here short-circuits with serviceChanged=false.
                var wasSoftDeleted = existingService.DeletedAtUtc is not null;
                if (wasSoftDeleted)
                {
                    existingService.DeletedAtUtc = null;
                }
                serviceChanged = wasSoftDeleted
                              || existingService.Port != serviceInfo.Port
                              || existingService.Protocol != serviceInfo.Protocol;
                existingService.Port = serviceInfo.Port;
                existingService.Protocol = serviceInfo.Protocol;
                serviceToProcess = existingService;
            }
            else if (portToServiceName.TryGetValue(serviceInfo.Port, out var oldServiceName))
            {
                var serviceToRename = existingServices[oldServiceName];

                var domainsToUpdate = project.Domains?
                    .Where(d => d.ServiceName == oldServiceName && d.Port == serviceInfo.Port)
                    .ToList();

                if (domainsToUpdate != null)
                {
                    foreach (var domain in domainsToUpdate)
                    {
                        domain.ServiceName = serviceInfo.Name;
                        domain.Port = serviceInfo.Port;
                        domain.Protocol = serviceInfo.Protocol;
                    }
                }

                serviceToRename.Name = serviceInfo.Name;
                serviceToRename.Port = serviceInfo.Port;
                serviceToRename.Protocol = serviceInfo.Protocol;
                serviceToProcess = serviceToRename;
            }
            else
            {
                serviceToProcess = new ProjectService
                {
                    ProjectId = command.ProjectId,
                    ProjectEnvironmentId = command.EnvironmentId,
                    Name = serviceInfo.Name,
                    Port = serviceInfo.Port,
                    Protocol = serviceInfo.Protocol,
                    IsSystem = SystemServices.Contains(serviceInfo.Name)
                };
                isNewService = true;
            }

            if (serviceChanged)
            {
                serviceToProcess.QueueDomainEvent<ProjectServiceUpsertedEvent>();
            }

            if (isNewService)
            {
                await projectServiceRepository.AddAsync(
                    serviceToProcess,
                    cancellationToken);
            }
            else if (serviceChanged)
            {
                await projectServiceRepository.UpdateAsync(
                    serviceToProcess,
                    cancellationToken);
            }
        }

        // Soft-delete internal domains in the reporting env whose service is no
        // longer in the report. This replaces the old SystemServices-only check
        // that missed custom service names like "Web" or "App", leaving orphaned
        // internal domains that caused routing conflicts in Traefik.
        //
        // Only InternalDomain records are touched — ExternalDomain (custom domains
        // like blog.mappso.com) are never auto-deleted; they're user-managed.
        //
        // Defense-in-depth: re-apply soft-delete and env-scope filters explicitly
        // because EF Core relationship fix-up can re-attach domains from other
        // queries in the same scoped DbContext (see earlier comment).
        var reportedServiceNames = command.Services.Select(s => s.Name).ToHashSet();
        var staleInternalDomains = project.Domains?
            .OfType<InternalDomain>()
            .Where(d => d.DeletedAtUtc == null
                     && d.ProjectEnvironmentId == command.EnvironmentId
                     && !reportedServiceNames.Contains(d.ServiceName))
            .ToList();

        if (staleInternalDomains != null && staleInternalDomains.Count > 0)
        {
            Log.ForContext("projectId", command.ProjectId)
               .ForContext("environmentId", command.EnvironmentId)
               .ForContext("staleDomainServices", staleInternalDomains.Select(d => $"{d.ServiceName}:{d.Port}").ToArray())
               .Information("Removing {Count} stale internal domains from env-scoped report", staleInternalDomains.Count);

            await staleInternalDomains.Select(x => x.Id)
                                      .ApplyAsync(domainRepository.DeleteAsync, cancellationToken);
        }

        // Bug 9: Clean up stale IsSystem=true services in the reporting env whose
        // name is no longer in the report. This matters when a project's type
        // changes (e.g. from Compozerr → DockerCompose) and the old type's
        // default system services linger forever otherwise. Only IsSystem rows
        // are deleted — user-created services are intentionally left alone
        // because port detection is best-effort and a missing report doesn't
        // always mean "gone".
        var staleSystemServices = existingServices.Values
            .Where(s => s.IsSystem && !reportedServiceNames.Contains(s.Name))
            .ToList();

        if (staleSystemServices.Count > 0)
        {
            Log.ForContext("projectId", command.ProjectId)
                .ForContext("environmentId", command.EnvironmentId)
                .ForContext("staleServiceNames", staleSystemServices.Select(s => s.Name).ToArray())
                .Information("Removing {Count} stale IsSystem services from env-scoped report", staleSystemServices.Count);

            await staleSystemServices.Select(s => s.Id)
                                     .ApplyAsync(projectServiceRepository.DeleteAsync, cancellationToken);
        }

        return new ReportServicesResponse();
    }
}
