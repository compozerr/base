using Api.Abstractions;
using Api.Data;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed class GetEnvironmentsCommandHandler(
    ApiDbContext dbContext) : ICommandHandler<GetEnvironmentsCommand, GetEnvironmentsResponse>
{
    public async Task<GetEnvironmentsResponse> Handle(
        GetEnvironmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var environments = await dbContext.ProjectEnvironments
            .Where(e => e.ProjectId == command.ProjectId)
            .Include(e => e.Deployments!.OrderByDescending(d => d.CreatedAtUtc).Take(1))
            .OrderBy(e => e.Type)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var dtos = environments.Select(e => new EnvironmentDto(
            e.Id.Value,
            e.Name ?? (e.Type == EnvironmentType.Production ? "Production" : $"Preview ({string.Join(", ", e.Branches)})"),
            e.Type.ToString(),
            e.Branches,
            e.AutoDeploy,
            e.PullRequestNumber,
            e.Subdomain,
            e.CreatedAtUtc,
            e.Deployments?.FirstOrDefault()?.CreatedAtUtc
        )).ToList();

        return new GetEnvironmentsResponse(dtos);
    }
}
