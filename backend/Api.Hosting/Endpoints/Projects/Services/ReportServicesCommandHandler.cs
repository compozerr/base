using Api.Data;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Endpoints.Projects.Services;

public sealed class ReportServicesCommandHandler(ApiDbContext context)
    : ICommandHandler<ReportServicesCommand, ReportServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<ReportServicesResponse> Handle(
        ReportServicesCommand command,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.ProjectServices)
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
            throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? new Dictionary<string, ProjectService>();

        var servicesUpdated = 0;

        foreach (var serviceInfo in command.Services)
        {
            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
                // Update port or protocol if changed
                var updated = false;
                if (existingService.Port != serviceInfo.Port)
                {
                    existingService.Port = serviceInfo.Port;
                    updated = true;
                }
                if (existingService.Protocol != serviceInfo.Protocol)
                {
                    existingService.Protocol = serviceInfo.Protocol;
                    updated = true;
                }
                if (updated)
                {
                    servicesUpdated++;
                }
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

        await context.SaveChangesAsync(cancellationToken);

        return new ReportServicesResponse(servicesUpdated);
    }
}
