namespace Api.Endpoints.Projects.Domains.Get;

public sealed record GetDomainDto(string ServiceName, string Value);
public sealed record GetDomainsResponse(
    List<GetDomainDto> Domains
);