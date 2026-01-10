using Api.Abstractions;
using DnsClient;

namespace Api.Endpoints.Projects.Domains.Lookup;

public static class GetDnsLookupRoute
{
    public const string Route = "lookup";

    public static RouteHandlerBuilder AddGetDnsLookupRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<DnsLookupResponse> ExecuteAsync(
        ProjectId projectId,
        [AsParameters] DnsLookupQuery query,
        ILookupClient lookupClient,
        CancellationToken cancellationToken)
    {
        var records = new List<DnsRecordInfo>();
        var domainName = query.Domain;

        try
        {
            // Check for A records
            var aResult = await lookupClient.QueryAsync(domainName, QueryType.A, cancellationToken: cancellationToken);
            foreach (var record in aResult.Answers.ARecords())
            {
                records.Add(new DnsRecordInfo("A", domainName, record.Address.ToString(), record.TimeToLive));
            }

            // Check for AAAA records
            var aaaaResult = await lookupClient.QueryAsync(domainName, QueryType.AAAA, cancellationToken: cancellationToken);
            foreach (var record in aaaaResult.Answers.AaaaRecords())
            {
                records.Add(new DnsRecordInfo("AAAA", domainName, record.Address.ToString(), record.TimeToLive));
            }

            // Check for CNAME records
            var cnameResult = await lookupClient.QueryAsync(domainName, QueryType.CNAME, cancellationToken: cancellationToken);
            foreach (var record in cnameResult.Answers.CnameRecords())
            {
                records.Add(new DnsRecordInfo("CNAME", domainName, record.CanonicalName.ToString().TrimEnd('.'), record.TimeToLive));
            }
        }
        catch (DnsResponseException ex)
        {
            Log.Warning(ex, "DNS lookup failed for domain {Domain}", domainName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unexpected error during DNS lookup for domain {Domain}", domainName);
        }

        return new DnsLookupResponse(records);
    }
}

public sealed record DnsLookupQuery(string Domain);
