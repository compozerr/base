using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Data.Services;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed class GetEnvironmentsCommandHandler(
    IProjectEnvironmentRepository environmentRepository,
    IServerVisibilityFilter serverVisibilityFilter) : ICommandHandler<GetEnvironmentsCommand, GetEnvironmentsResponse>
{
    public async Task<GetEnvironmentsResponse> Handle(
        GetEnvironmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        var environments = await environmentRepository.GetByProjectIdWithLatestDeploymentAsync(
            command.ProjectId, cancellationToken);

        var dtos = environments.Select(e =>
        {
            var serverTierId = e.ServerTierId?.Value;
            decimal? monthlyPrice = null;

            if (e.ServerTierId is not null)
            {
                try
                {
                    monthlyPrice = ServerTiers.GetById(e.ServerTierId).Price.Value;
                }
                catch (ArgumentException)
                {
                    // Gracefully handle unknown tier IDs on existing environments
                }
            }

            // Mask servers that aren't visible to the caller (AdminOnly servers for
            // non-admins). Filtered per-env: other envs may keep their visible server.
            var server = e.Project?.Server;
            var visibleServer = server is null
                ? null
                : serverVisibilityFilter.ApplyForCurrentUser(new[] { server }).FirstOrDefault();
            var allocatedServer = visibleServer is null
                ? null
                : new AllocatedServerSummary(
                    visibleServer.Id,
                    visibleServer.HostName,
                    visibleServer.Location?.IsoCountryCode);

            return new EnvironmentDto(
                e.Id.Value,
                e.Name ?? (e.Type == EnvironmentType.Production ? "Production" : $"Preview ({string.Join(", ", e.Branches)})"),
                e.Type.ToString(),
                e.Branches,
                e.AutoDeploy,
                e.EffectiveAutoDeploy,
                e.PullRequestNumber,
                e.Subdomain,
                e.GetUrl(),
                e.CreatedAtUtc,
                e.Deployments?.FirstOrDefault()?.CreatedAtUtc,
                serverTierId,
                monthlyPrice,
                e.BillingStatus.ToString(),
                e.StripeSubscriptionId,
                e.HealthStatus,
                e.LastHealthCheckAtUtc,
                allocatedServer
            );
        }).ToList();

        return new GetEnvironmentsResponse(dtos);
    }
}
