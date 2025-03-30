namespace Api.Endpoints.Projects.Domains.Get;

public sealed record DomainDto(string ServiceName, string Value);
public sealed record GetDomainsResponse(
    List<DomainDto> Domains
);