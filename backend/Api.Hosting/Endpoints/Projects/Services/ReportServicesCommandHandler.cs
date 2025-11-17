using Api.Data;
using Api.Data.Repositories;
using Core.Extensions;
using Core.MediatR;
using Database.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed class ReportServicesCommandHandler(
    IProjectServiceRepository projectServiceRepository,
    IProjectRepository projectRepository,
    IDomainRepository domainRepository)
    : ICommandHandler<ReportServicesCommand, ReportServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<ReportServicesResponse> Handle(
        ReportServicesCommand command,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetSingleAsync(
            x => x.Id == command.ProjectId,
            i => i.Include(x => x.ProjectServices)
                    .Include(x => x.Domains), cancellationToken)
                    ?? throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? [];

        var portToServiceName = project.ProjectServices?.ToDictionary(s => s.Port, s => s.Name)
            ?? [];

        foreach (var serviceInfo in command.Services)
        {
            ProjectService serviceToProcess;
            bool isNewService = false;

            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
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
                    Name = serviceInfo.Name,
                    Port = serviceInfo.Port,
                    Protocol = serviceInfo.Protocol,
                    IsSystem = SystemServices.Contains(serviceInfo.Name)
                };
                isNewService = true;
            }

            serviceToProcess.QueueDomainEvent<ProjectServiceUpsertedEvent>();

            if (isNewService)
            {
                await projectServiceRepository.AddAsync(
                    serviceToProcess,
                    cancellationToken);
            }
            else
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
