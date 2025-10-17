using Api.Data;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.Upsert;

public sealed class UpsertProjectServicesCommandHandler(ApiDbContext context)
    : ICommandHandler<UpsertProjectServicesCommand, UpsertProjectServicesResponse>
{
    private static readonly string[] SystemServices = ["Frontend", "Backend"];

    public async Task<UpsertProjectServicesResponse> Handle(
        UpsertProjectServicesCommand command,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.ProjectServices)
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
            throw new InvalidOperationException($"Project with ID {command.ProjectId} not found");

        var existingServices = project.ProjectServices?.ToDictionary(s => s.Name, s => s)
            ?? new Dictionary<string, ProjectService>();

        foreach (var serviceInfo in command.Services)
        {
            if (existingServices.TryGetValue(serviceInfo.Name, out var existingService))
            {
                existingService.Port = serviceInfo.Port;
                existingService.Protocol = serviceInfo.Protocol;
            }
            else
            {
                var newService = new ProjectService
                {
                    ProjectId = command.ProjectId,
                    Name = serviceInfo.Name,
                    Port = serviceInfo.Port,
                    Protocol = serviceInfo.Protocol,
                    IsSystem = SystemServices.Contains(serviceInfo.Name)
                };

                context.ProjectServices.Add(newService);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return new UpsertProjectServicesResponse();
    }
}
