using Api.Data;

namespace Api.Endpoints.Projects.Environments.Get;

public sealed record GetEnvironmentsResponse(
    List<EnvironmentDto> Environments);

public sealed record EnvironmentDto(
    Guid Id,
    string Name,
    string Type,
    List<string> Branches,
    bool AutoDeploy,
    bool EffectiveAutoDeploy,
    int? PullRequestNumber,
    string? Subdomain,
    string? Url,
    DateTime CreatedAtUtc,
    DateTime? LastDeploymentAtUtc,
    string? ServerTierId,
    decimal? MonthlyPrice,
    string? BillingStatus,
    string? StripeSubscriptionId,
    EnvironmentHealthStatus HealthStatus,
    DateTimeOffset? LastHealthCheckAtUtc);
