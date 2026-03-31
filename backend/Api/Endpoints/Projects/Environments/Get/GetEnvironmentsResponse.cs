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
    int? PullRequestNumber,
    string? Subdomain,
    DateTime CreatedAtUtc,
    DateTime? LastDeploymentAtUtc);
