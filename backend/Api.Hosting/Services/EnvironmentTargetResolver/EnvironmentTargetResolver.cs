using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Services.EnvironmentTargetResolver;

public sealed class EnvironmentTargetResolver(
    ApiDbContext context,
    IHostingApiFactory hostingApiFactory) : IEnvironmentTargetResolver
{
    public async Task<IHostingApi> GetHostingApiAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default)
    {
        var server = await GetServerAsync(environmentId, cancellationToken);
        return await hostingApiFactory.GetHostingApiAsync(server.Id);
    }

    public async Task<Server> GetServerAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default)
    {
        var server = await TryGetServerAsync(environmentId, cancellationToken);
        if (server is null)
            throw new ServerNotFoundException();
        return server;
    }

    public async Task<Server?> TryGetServerAsync(ProjectEnvironmentId environmentId, CancellationToken cancellationToken = default)
    {
        var env = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.Id == environmentId)
            .Select(e => new { e.ServerId, ProjectServerId = e.Project!.ServerId })
            .FirstOrDefaultAsync(cancellationToken);

        if (env is null) return null;

        // Prefer env-level ServerId; fall back to project-level hint during the
        // transition window in which envs created before s-b1 may not yet have
        // a server assigned.
        var serverId = env.ServerId ?? env.ProjectServerId;
        if (serverId is null) return null;

        return await LoadServerAsync(serverId, cancellationToken);
    }

    public async Task<Server> GetServerForDomainAsync(DomainId domainId, CancellationToken cancellationToken = default)
    {
        var domain = await context.Domains
            .AsNoTracking()
            .Where(d => d.Id == domainId)
            .Select(d => new { d.ProjectId, d.ProjectEnvironmentId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ArgumentException($"Domain {domainId} not found");

        if (domain.ProjectEnvironmentId is { } envId)
        {
            // F-P1-1 (defense in depth): the env a domain points at MUST belong
            // to the domain's own project. Without this guard a caller who can
            // set Domain.ProjectEnvironmentId (Add/ChangeEnvironment handlers)
            // could pin project A's domain to project B's env and route traffic
            // to B's server. Fail loudly on a cross-project reference rather
            // than silently falling through to the project hint.
            var ownerProjectId = await context.ProjectEnvironments
                .AsNoTracking()
                .Where(e => e.Id == envId)
                .Select(e => e.ProjectId)
                .FirstOrDefaultAsync(cancellationToken);

            if (ownerProjectId is { } pid && pid != domain.ProjectId)
                throw new InvalidOperationException(
                    $"Domain {domainId} references environment {envId} that does not belong to project {domain.ProjectId}");

            var envServer = await TryGetServerAsync(envId, cancellationToken);
            if (envServer is not null) return envServer;
        }

        // Apex / project-level domain: fall back to production env's server,
        // then to legacy project hint.
        return await GetProductionOrHintServerAsync(domain.ProjectId, cancellationToken);
    }

    public async Task<Server> GetServerForServiceAsync(ProjectId projectId, string serviceName, CancellationToken cancellationToken = default)
    {
        var serviceEnvId = await context.ProjectServices
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId && s.Name == serviceName && s.DeletedAtUtc == null)
            .Select(s => s.ProjectEnvironmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (serviceEnvId is { } envId)
        {
            var envServer = await TryGetServerAsync(envId, cancellationToken);
            if (envServer is not null) return envServer;
        }

        return await GetProductionOrHintServerAsync(projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<Server>> GetDistinctServersForProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        var serverIds = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId && e.ServerId != null)
            .Select(e => e.ServerId!)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Include the project's legacy ServerId hint so envs that haven't been
        // backfilled still get their server included in the fan-out.
        var projectHint = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => p.ServerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (projectHint is not null && !serverIds.Contains(projectHint))
            serverIds.Add(projectHint);

        if (serverIds.Count == 0) return Array.Empty<Server>();

        return await context.Servers
            .AsNoTracking()
            .Include(s => s.Location)
            .Where(s => serverIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IHostingApi>> GetDistinctHostingApisForProjectAsync(ProjectId projectId, CancellationToken cancellationToken = default)
    {
        var servers = await GetDistinctServersForProjectAsync(projectId, cancellationToken);
        var apis = new List<IHostingApi>(servers.Count);
        foreach (var server in servers)
        {
            apis.Add(await hostingApiFactory.GetHostingApiAsync(server.Id));
        }
        return apis;
    }

    private async Task<Server> GetProductionOrHintServerAsync(ProjectId projectId, CancellationToken cancellationToken)
    {
        var prodEnvServerId = await context.ProjectEnvironments
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId && e.Type == EnvironmentType.Production && e.ServerId != null)
            .Select(e => e.ServerId!)
            .FirstOrDefaultAsync(cancellationToken);

        var projectHint = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => p.ServerId)
            .FirstOrDefaultAsync(cancellationToken);

        var serverId = prodEnvServerId ?? projectHint
            ?? throw new ServerNotFoundException();

        return await LoadServerAsync(serverId, cancellationToken);
    }

    private async Task<Server> LoadServerAsync(ServerId serverId, CancellationToken cancellationToken)
    {
        return await context.Servers
            .AsNoTracking()
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.Id == serverId, cancellationToken)
            ?? throw new ServerNotFoundException();
    }
}
