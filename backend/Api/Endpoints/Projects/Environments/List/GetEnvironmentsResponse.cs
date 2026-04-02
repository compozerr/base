using Api.Data;

namespace Api.Endpoints.Projects.Environments.List;

public sealed record GetEnvironmentDto(
    Guid Id,
    string Name,
    string Branch,
    EnvironmentType Type,
    EnvironmentStatus Status,
    string? VmIdentifier,
    string? Subdomain,
    bool AutoDestroy,
    int? TtlDays,
    DateTime CreatedAtUtc);

public sealed record GetEnvironmentsResponse(
    List<GetEnvironmentDto> Environments);
