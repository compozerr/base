using Api.Data;

namespace Api.Endpoints.Projects.Domains.Get;

public sealed record GetDomainDto(
    Guid DomainId,
    string ServiceName,
    Protocol Protocol,
    string Value,
    bool IsInternal,
    bool IsVerified,
    bool IsPrimary,
    Guid? EnvironmentId = null,
    string? EnvironmentName = null);

public sealed record GetDomainsResponse(
    List<GetDomainDto> Domains
);