using Api.Data;
using Api.Data.Repositories;
using Core.Extensions;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed class ReportServicesCommandHandler(
    IProjectServiceRepository projectServiceRepository,
    IProjectRepository projectRepository,
    IDomainRepository domainRepository,
    ApiDbContext context)
    : ICommandHandler<ReportServicesCommand, ReportServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<ReportServicesResponse> Handle(
        ReportServicesCommand command,
        CancellationToken cancellationToken)
    {
        // Legacy non-env-scoped endpoint. Kept for backwards compat with old hosting
        // managers — always scopes to the project's production environment. Once
        // every hosting agent passes environmentId, this endpoint becomes dead code.
        var productionEnvId = await context.Set<Api.Data.ProjectEnvironment>()
            .Where(e => e.ProjectId == command.ProjectId
                     && e.Type == EnvironmentType.Production
                     && e.DeletedAtUtc == null)
            .Select(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                $"Project with ID {command.ProjectId} has no production environment");

        var project = await projectRepository.GetSingleAsync(
            x => x.Id == command.ProjectId,
            i => i.Include(x => x.ProjectServices!.Where(s => s.ProjectEnvironmentId == productionEnvId))
                    .Include(x => x.Domains!.Where(d => d.DeletedAtUtc == null
                                                     && d.ProjectEnvironmentId == productionEnvId)), cancellationToken)
                    ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? [];

        // Port-based rename detection across the prod env. GroupBy + First handles
        // the (theoretical) case of two prod services sharing a port.
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
                serviceChanged = existingService.Port != serviceInfo.Port
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
                    ProjectEnvironmentId = productionEnvId,
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

        // Remove system domains in prod env that have no corresponding open port.
        // Defense-in-depth: the outer filtered Include scopes the load, but EF Core
        // relationship fix-up can re-attach soft-deleted or cross-env domains that
        // were loaded elsewhere in the same scoped DbContext (e.g. the
        // ProjectServiceUpsertedEvent handler's GetProjectByIdWithDomainsAsync
        // call). Re-applying the filters in memory here keeps DeleteAsync from
        // crashing on a fixed-up row. See ReportEnvironmentServicesCommandHandler
        // for the matching fix on the env-scoped endpoint.
        var reportedPorts = command.Services.Select(s => s.Port).ToHashSet();
        var systemDomainsToRemove = project.Domains?
            .Where(d => d.DeletedAtUtc == null
                     && d.ProjectEnvironmentId == productionEnvId
                     && SystemServices.Contains(d.ServiceName)
                     && !reportedPorts.Contains(d.Port))
            .ToList();

        if (systemDomainsToRemove != null && systemDomainsToRemove.Count > 0)
        {
            await systemDomainsToRemove.Select(x => x.Id)
                                       .ApplyAsync(domainRepository.DeleteAsync, cancellationToken);
        }

        // Bug 9: Clean up stale IsSystem=true services in prod env whose name is
        // no longer in the report. Matches the env-scoped handler — see its
        // comment for the full rationale. The legacy endpoint only scopes to
        // the prod env, so this cleanup is likewise scoped.
        var reportedServiceNames = command.Services.Select(s => s.Name).ToHashSet();
        var staleSystemServices = existingServices.Values
            .Where(s => s.IsSystem && !reportedServiceNames.Contains(s.Name))
            .ToList();

        if (staleSystemServices.Count > 0)
        {
            Log.ForContext("projectId", command.ProjectId)
                .ForContext("productionEnvId", productionEnvId)
                .ForContext("staleServiceNames", staleSystemServices.Select(s => s.Name).ToArray())
                .Information("Removing {Count} stale IsSystem services from legacy prod-env report", staleSystemServices.Count);

            await staleSystemServices.Select(s => s.Id)
                                     .ApplyAsync(projectServiceRepository.DeleteAsync, cancellationToken);
        }

        return new ReportServicesResponse();
    }
}
