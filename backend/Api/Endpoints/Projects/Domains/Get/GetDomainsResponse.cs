using Api.Abstractions;

namespace Api.Endpoints.Projects.Domains.Get;

public sealed record GetDomainDto(
    DomainId DomainId,
    string ServiceName,
    string Value,
    bool IsInternal,
    bool IsVerified);

public sealed record GetDomainsResponse(
    List<GetDomainDto> Domains
);