using Api.Abstractions.Exceptions;
using Api.Data;
using Api.Data.Events;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Domains.Verify;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Database.Extensions;
using DnsClient;
using Jobs;
using Microsoft.EntityFrameworkCore;

namespace Api.Jobs;

public class DomainVerificationJob(
    IDomainRepository domainRepository,
    IEnvironmentTargetResolver environmentTargetResolver,
    ILookupClient lookupClient,
    IHttpClientFactory httpClientFactory) : JobBase<DomainVerificationJob>
{
    public override async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        // Get all unverified external domains that are due for a check.
        // The env's server is resolved per-domain inside VerifyDomainAsync via
        // the resolver — no legacy server Include needed here.
        var domainsToCheck = await domainRepository.Query()
            .OfType<ExternalDomain>()
            .Where(d => !d.IsVerified)
            .Where(d => d.NextVerificationCheckUtc == null || d.NextVerificationCheckUtc <= now)
            .ToListAsync();

        Log.Information("Found {Count} domains to verify", domainsToCheck.Count);

        foreach (var domain in domainsToCheck)
        {
            try
            {
                await VerifyDomainAsync(domain, now);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error verifying domain {DomainId}", domain.Id);
            }
        }
    }

    private async Task VerifyDomainAsync(ExternalDomain domain, DateTime now)
    {
        var recordName = domain.Value;
        var isApex = DomainExtensions.IsApexDomain(recordName);

        // Get parent domain for verification target
        var parentDomain = await domainRepository.GetParentDomainAsync(domain.Id, CancellationToken.None);

        bool isVerified;

        if (isApex)
        {
            // For apex domains, verify A record pointing to the env's server IP.
            string? serverIp = null;
            try
            {
                var server = await environmentTargetResolver.GetServerForDomainAsync(domain.Id);
                serverIp = server.Ip;
            }
            catch (ServerNotFoundException)
            {
                // No server reachable from any env or legacy hint — treat as
                // "IP not available" (the prior failure mode).
            }

            if (string.IsNullOrEmpty(serverIp))
            {
                Log.Warning("Server IP not available for A record verification of domain {DomainId}", domain.Id);
                UpdateCheckSchedule(domain, now, false);
                await domainRepository.UpdateAsync(domain);
                return;
            }

            isVerified = await GetVerifyRoute.VerifyARecordAsync(
                lookupClient,
                recordName,
                serverIp);
        }
        else
        {
            // For subdomains, verify CNAME record
            var recordValue = parentDomain.GetValue;
            isVerified = await GetVerifyRoute.VerifyCnameRecordAsync(
                lookupClient,
                recordName,
                recordValue);
        }

        UpdateCheckSchedule(domain, now, isVerified);

        if (isVerified)
        {
            // Perform health check before marking as verified
            var healthCheckPassed = await PerformHealthCheckAsync(domain);
            domain.HealthCheckPassed = healthCheckPassed;

            if (healthCheckPassed)
            {
                domain.IsVerified = true;
                domain.QueueDomainEvent<DomainVerifiedEvent>();
                Log.Information("Domain {DomainId} ({Value}) verified and healthy", domain.Id, domain.Value);
            }
            else
            {
                Log.Information("Domain {DomainId} ({Value}) DNS verified but health check failed", domain.Id, domain.Value);
            }
        }

        await domainRepository.UpdateAsync(domain);
    }

    private static void UpdateCheckSchedule(ExternalDomain domain, DateTime now, bool verified)
    {
        domain.VerificationCheckCount++;
        domain.LastVerificationCheckUtc = now;

        if (verified)
        {
            // No more checks needed
            domain.NextVerificationCheckUtc = null;
            return;
        }

        // Calculate next check based on adaptive schedule
        var interval = GetNextCheckInterval(domain.VerificationCheckCount);
        domain.NextVerificationCheckUtc = now.Add(interval);
    }

    /// <summary>
    /// Adaptive check rate schedule:
    /// - Checks 1-3: Every 5 minutes (DNS can propagate quickly)
    /// - Checks 4-6: Every 30 minutes (give time for slower providers)
    /// - Checks 7-12: Every 2 hours (reduce load, user likely waiting)
    /// - Checks 13-24: Every 6 hours (long-term monitoring)
    /// - Checks 25+: Every 24 hours (minimal background checking)
    /// </summary>
    private static TimeSpan GetNextCheckInterval(int checkCount)
    {
        return checkCount switch
        {
            <= 3 => TimeSpan.FromMinutes(5),
            <= 6 => TimeSpan.FromMinutes(30),
            <= 12 => TimeSpan.FromHours(2),
            <= 24 => TimeSpan.FromHours(6),
            _ => TimeSpan.FromHours(24)
        };
    }

    private async Task<bool> PerformHealthCheckAsync(ExternalDomain domain)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            // Try HTTPS first, fallback to HTTP
            var protocols = new[] { "https", "http" };

            foreach (var protocol in protocols)
            {
                try
                {
                    var url = $"{protocol}://{domain.Value}";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch (HttpRequestException)
                {
                    // Try next protocol
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Health check failed for domain {DomainId}", domain.Id);
            return false;
        }
    }
}
