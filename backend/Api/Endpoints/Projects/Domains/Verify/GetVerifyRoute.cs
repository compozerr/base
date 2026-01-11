using Api.Abstractions;
using Api.Data;
using Api.Data.Events;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Database.Extensions;
using DnsClient;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Domains.Verify;

public static class GetVerifyRoute
{
    public const string Route = "{domainId:guid}/verify";

    public static RouteHandlerBuilder AddGetVerifyRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<bool> ExecuteAsync(
        ProjectId projectId,
        DomainId domainId,
        IDomainRepository domainRepository,
        ILookupClient lookupClient,
        CancellationToken cancellationToken)
    {
        var domain = await domainRepository.GetByIdAsync(
            domainId,
            cancellationToken) ??
            throw new ArgumentException($"Domain with ID '{domainId}' not found.");

        if (domain is ExternalDomain { IsVerified: true })
        {
            Log.Information("Domain is already verified");
            return true;
        }

        var parentDomain = await domainRepository.GetParentDomainAsync(
            domain.Id,
            cancellationToken);

        var recordName = domain.GetValue;
        var isApex = DomainExtensions.IsApexDomain(recordName);

        bool isVerified;

        if (isApex)
        {
            // For apex domains, verify A record pointing to server IP
            var domainWithServer = await domainRepository.Query()
                .Include(d => d.Project)
                .ThenInclude(p => p!.Server)
                .FirstOrDefaultAsync(d => d.Id == parentDomain.Id, cancellationToken);

            var serverIp = domainWithServer?.Project?.Server?.Ip;

            if (string.IsNullOrEmpty(serverIp))
            {
                Log.Warning("Server IP not available for A record verification");
                return false;
            }

            isVerified = await VerifyARecordAsync(
                lookupClient,
                recordName,
                serverIp,
                cancellationToken);
        }
        else
        {
            // For subdomains, verify CNAME record
            var recordValue = parentDomain.GetValue;
            isVerified = await VerifyCnameRecordAsync(
                lookupClient,
                recordName,
                recordValue,
                cancellationToken);
        }

        if (isVerified && domain is ExternalDomain externalDomain)
        {
            externalDomain.IsVerified = isVerified;
            externalDomain.QueueDomainEvent<DomainChangeEvent>();

            await domainRepository.UpdateAsync(
                externalDomain,
                cancellationToken);
        }

        return isVerified;
    }

    public static async Task<bool> VerifyARecordAsync(
        ILookupClient lookupClient,
        string recordName,
        string expectedIp,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryResult = await lookupClient.QueryAsync(
                recordName,
                QueryType.A,
                cancellationToken: cancellationToken);

            var aRecords = queryResult.Answers.ARecords();

            foreach (var record in aRecords)
            {
                if (record.Address.ToString().Equals(expectedIp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            Log.Warning("Required A record not found or points to incorrect IP address");
            return false;
        }
        catch (DnsResponseException ex)
        {
            Log.ForContext(nameof(ex), ex).Warning("DNS lookup failed for A record");
            return false;
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(ex), ex).Warning("A record verification failed");
            return false;
        }
    }

    public static async Task<bool> VerifyCnameRecordAsync(
        ILookupClient lookupClient,
        string recordName,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the expected CNAME record details from the domain entity
            var queryResult = await lookupClient.QueryAsync(
                recordName,
                QueryType.CNAME,
                cancellationToken: cancellationToken);

            // Check if the CNAME record exists and contains the expected value
            var cnameRecords = queryResult.Answers.CnameRecords();

            foreach (var record in cnameRecords)
            {
                // The CNAME record has a CanonicalName property that contains the target domain
                if (record.CanonicalName.ToString().TrimEnd('.').Equals(value.TrimEnd('.'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // If we reached here, verification failed
            Log.Warning("Required CNAME record not found or points to incorrect value");
            return false;
        }
        catch (DnsResponseException ex)
        {
            Log.ForContext(nameof(ex), ex).Warning("Dns lookup failed");
            return false;
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(ex), ex).Warning("Verification failed");
            return false;
        }

    }
}