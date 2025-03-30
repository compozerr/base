using Api.Abstractions;
using Api.Data;
using Api.Data.Events;
using Api.Data.Repositories;
using Database.Extensions;
using DnsClient;

namespace Api.Endpoints.Projects.Domains.Verify;

public static class GetVerifyRoute
{
    public const string Route = "{domainId:guid}/verify";

    public static RouteHandlerBuilder AddGetVerifyRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<bool> ExecuteAsync(
        Guid projectId,
        Guid domainId,
        IDomainRepository domainRepository,
        ILookupClient lookupClient,
        CancellationToken cancellationToken)
    {
        var convertedDomainId = DomainId.Create(domainId);

        var domain = await domainRepository.GetByIdAsync(
            convertedDomainId,
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
        var recordValue = parentDomain.GetValue;

        var isVerified = await VerifyCnameRecordAsync(
            lookupClient,
            recordName,
            recordValue,
            cancellationToken);


        if (domain is ExternalDomain externalDomain)
        {
            externalDomain.IsVerified = isVerified;
            externalDomain.QueueDomainEvent<DomainChangeEvent>();

            await domainRepository.UpdateAsync(
                externalDomain,
                cancellationToken);
        }

        return isVerified;
    }

    public async static Task<bool> VerifyCnameRecordAsync(
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