using Api.Data;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Services.Get;

public sealed class GetProjectServicesCommandHandler(ApiDbContext context)
    : ICommandHandler<GetProjectServicesCommand, GetProjectServicesResponse>
{
    public async Task<GetProjectServicesResponse> Handle(
        GetProjectServicesCommand command,
        CancellationToken cancellationToken)
    {
        var services = await context.ProjectServices
            .Where(s => s.ProjectId == command.ProjectId)
            .OrderByDescending(s => s.IsSystem)
            .ThenBy(s => s.Name)
            .Select(s => new ProjectServiceDto(s.Name, s.Port, s.IsSystem))
            .ToListAsync(cancellationToken);

        return new GetProjectServicesResponse(services);
    }
}
