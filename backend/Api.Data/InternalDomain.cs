namespace Api.Data;

public sealed class InternalDomain : Domain
{
    public InternalDomain()
    {
        Type = DomainType.Internal;
    }

    public required string Subdomain { get; set; } = string.Empty;

    public string Value
    {
        get
        {
            var hostName = ResolveHostName()
                ?? throw new InvalidOperationException("InternalDomain hostname cannot be resolved");
            return $"{Subdomain}.sites.{hostName}.compozerr.com";
        }
    }

    public override string GetValue => Value;

    // The hostname resolution walks loaded navigations in priority order:
    //   1. This domain's env's server (per-env routing — post-Spec-B normal case).
    //   2. The project's Production env's server (apex / project-level domains
    //      whose ProjectEnvironmentId is null fall back to prod env).
    //   3. The project's legacy ServerId hint (transition: when an env hasn't
    //      been backfilled to carry its own ServerId yet, or callers haven't
    //      loaded the env-server navs).
    // Returns null when no server is reachable from any loaded nav — callers
    // (via Value) must treat that as a hard failure rather than emitting a
    // malformed "subdomain.sites..compozerr.com" string.
    private string? ResolveHostName()
    {
        if (ProjectEnvironment?.Server?.HostName is { Length: > 0 } envHost)
            return envHost;

        if (Project?.ProjectEnvironments is { } envs)
        {
            var prodHost = envs
                .FirstOrDefault(e => e.Type == EnvironmentType.Production)?
                .Server?.HostName;
            if (!string.IsNullOrEmpty(prodHost)) return prodHost;
        }

        return string.IsNullOrEmpty(Project?.Server?.HostName) ? null : Project!.Server!.HostName;
    }
}
