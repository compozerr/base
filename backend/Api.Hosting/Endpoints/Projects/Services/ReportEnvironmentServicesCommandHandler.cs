using Api.Data;
using Api.Data.Repositories;
using Core.Extensions;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

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
        // Load ALL services for the project (not just those in the reporting env).
        // The (ProjectId, Name) unique index forces a project-wide upsert-by-name, so
        // filtering out rows from other envs would hit a unique-constraint violation
        // on insert. Domains are kept env-filtered — they have no cross-env uniqueness
        // and the system-domain cleanup below only concerns the reporting env.
        var project = await projectRepository.GetSingleAsync(
            x => x.Id == command.ProjectId,
            i => i.Include(x => x.ProjectServices)
                    .Include(x => x.Domains!.Where(d => d.ProjectEnvironmentId == command.EnvironmentId)), cancellationToken)
                    ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? [];

        // Port-based rename detection only makes sense within the reporting env — a
        // same-port service in a different env is a coincidence, not a rename. Grouping
        // avoids a ToDictionary crash if two services in the same env share a port.
        var portToServiceName = project.ProjectServices?
            .Where(s => s.ProjectEnvironmentId == command.EnvironmentId)
            .GroupBy(s => s.Port)
            .ToDictionary(g => g.Key, g => g.First().Name) ?? [];

        foreach (var serviceInfo in command.Services)
        {
            ProjectService serviceToProcess;
            bool isNewService = false;
            bool serviceChanged = true;

            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
                // Claim the service for the reporting environment. Because of the
                // (ProjectId, Name) unique index, a project has at most one service
                // per name, so the env it belongs to is simply whichever env last
                // deployed it. This also heals rows with NULL ProjectEnvironmentId
                // that predate the environment-scoping migration.
                serviceChanged = existingService.Port != serviceInfo.Port
                              || existingService.Protocol != serviceInfo.Protocol
                              || existingService.ProjectEnvironmentId != command.EnvironmentId;
                existingService.Port = serviceInfo.Port;
                existingService.Protocol = serviceInfo.Protocol;
                existingService.ProjectEnvironmentId = command.EnvironmentId;
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
                serviceToRename.ProjectEnvironmentId = command.EnvironmentId;
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

        // Remove system domains that have no corresponding open port
        var reportedPorts = command.Services.Select(s => s.Port).ToHashSet();
        var systemDomainsToRemove = project.Domains?
            .Where(d => SystemServices.Contains(d.ServiceName) && !reportedPorts.Contains(d.Port))
            .ToList();

        if (systemDomainsToRemove != null && systemDomainsToRemove.Count > 0)
        {
            await systemDomainsToRemove.Select(x => x.Id)
                                       .ApplyAsync(domainRepository.DeleteAsync, cancellationToken);
        }

        return new ReportServicesResponse();
    }
}
