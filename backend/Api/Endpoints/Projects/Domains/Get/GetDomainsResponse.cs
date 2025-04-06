namespace Api.Endpoints.Projects.Domains.Get;

public sealed record GetDomainDto(
    Guid DomainId,
    string ServiceName,
    string Value,
    bool IsInternal,
    bool IsVerified,
    bool IsPrimary);

public sealed record GetDomainsResponse(
    List<GetDomainDto> Domains
);