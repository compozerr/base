using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed class ReportServicesCommandHandler(
    ApiDbContext context,
    IProjectRepository projectRepository)
    : ICommandHandler<ReportServicesCommand, ReportServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<ReportServicesResponse> Handle(
        ReportServicesCommand command,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetSingleAsync(x => x.Id == command.ProjectId, i => i.Include(x => x.ProjectServices).Include(x => x.Domains), cancellationToken);

        if (project is null)
            throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? new Dictionary<string, ProjectService>();

        // Create a map of port -> old service name for detecting name changes
        var portToServiceName = project.ProjectServices?.ToDictionary(s => s.Port, s => s.Name)
            ?? new Dictionary<string, string>();

        var servicesUpdated = 0;

        foreach (var serviceInfo in command.Services)
        {
            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
                existingService.Port = serviceInfo.Port;
                existingService.Protocol = serviceInfo.Protocol;
            }
            else
            {
                // Check if this port was used by a different service name
                if (portToServiceName.TryGetValue(serviceInfo.Port, out var oldServiceName))
                {
                    // Port exists but with a different name - rename the service
                    var serviceToRename = existingServices[oldServiceName];

                    // Update domains that use the old service name on this port
                    var domainsToUpdate = project.Domains?
                        .Where(d => d.ServiceName == oldServiceName && d.Port == serviceInfo.Port)
                        .ToList();

                    if (domainsToUpdate != null)
                    {
                        foreach (var domain in domainsToUpdate)
                        {
                            domain.ServiceName = serviceInfo.Name;
                            domain.Port = serviceInfo.Port;
                        }
                    }

                    // Remove old service and add new one
                    context.ProjectServices.Remove(serviceToRename);
                    context.ProjectServices.Add(new ProjectService
                    {
                        ProjectId = command.ProjectId,
                        Name = serviceInfo.Name,
                        Port = serviceInfo.Port,
                        Protocol = serviceInfo.Protocol,
                        IsSystem = SystemServices.Contains(serviceInfo.Name)
                    });

                    servicesUpdated++;
                }
                else
                {
                    // Add new service
                    var newService = new ProjectService
                    {
                        ProjectId = command.ProjectId,
                        Name = serviceInfo.Name,
                        Port = serviceInfo.Port,
                        Protocol = serviceInfo.Protocol,
                        IsSystem = SystemServices.Contains(serviceInfo.Name)
                    };

                    context.ProjectServices.Add(newService);
                    servicesUpdated++;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return new ReportServicesResponse(servicesUpdated);
    }
}
