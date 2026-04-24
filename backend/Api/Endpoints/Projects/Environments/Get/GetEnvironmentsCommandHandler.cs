using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed class GetEnvironmentsCommandHandler(
    IProjectEnvironmentRepository environmentRepository) : ICommandHandler<GetEnvironmentsCommand, GetEnvironmentsResponse>
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
                e.StripeSubscriptionId
            );
        }).ToList();

        return new GetEnvironmentsResponse(dtos);
    }
}
