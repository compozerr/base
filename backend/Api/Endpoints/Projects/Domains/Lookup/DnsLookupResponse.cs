namespace Api.Endpoints.Projects.Domains.Lookup;

public sealed record DnsLookupResponse(List<DnsRecordInfo> Records);

public sealed record DnsRecordInfo(string Type, string Name, string Value, int Ttl);
