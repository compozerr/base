namespace Api.Data;

public sealed class InternalDomain : Domain
{
    public InternalDomain()
    {
        Type = DomainType.Internal;
    }

    public required string Subdomain { get; set; } = string.Empty;

    public string Value => $"{Subdomain}.sites.{ResolveHostName()}.compozerr.com";

    public override string GetValue => Value;

    // The hostname resolution walks loaded navigations in priority order:
    //   1. This domain's env's server (per-env routing — post-Spec-B normal case).
    //   2. The project's Production env's server (apex / project-level domains
    //      whose ProjectEnvironmentId is null fall back to prod env).
    //   3. The project's legacy ServerId hint (transition: when an env hasn't
    //      been backfilled to carry its own ServerId yet, or callers haven't
    //      loaded the env-server navs).
    // Returning empty hostname is reserved for "no server reachable from any
    // loaded nav" — readers that hit this path should regard the URL as
    // unresolved rather than valid.
    private string ResolveHostName()
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

        return Project?.Server?.HostName ?? string.Empty;
    }
}
