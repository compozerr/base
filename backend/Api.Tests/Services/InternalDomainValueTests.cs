using Api.Abstractions;
using Api.Data;

namespace Api.Tests.Services;

/// <summary>
/// Post-Spec-B regression checks for <see cref="InternalDomain.Value"/>.
///
/// Each environment of a project can live on a different server, so an
/// env-pinned domain's hostname must come from that env's server.
/// Apex/project-level domains (ProjectEnvironmentId is null) fall back to
/// the project's Production env's server. The legacy
/// project-level ServerId hint is the last resort during the migration window.
/// </summary>
public class InternalDomainValueTests
{
    private static Server MakeServer(string hostName)
        => new()
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            HostName = hostName,
            ApiDomain = $"api.{hostName}.example.com",
            Ip = "10.0.0.1",
        };

    private static ProjectEnvironment MakeEnv(EnvironmentType type, Server? server)
        => new()
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = ProjectId.CreateNew(),
            Branches = new List<string>(),
            AutoDeploy = true,
            Type = type,
            Server = server,
            ServerId = server?.Id,
        };

    [Fact]
    public void Value_UsesEnvironmentServer_WhenDomainIsPinnedToEnv()
    {
        // Project P has prod env on Server A, preview env on Server B.
        // Each env's InternalDomain must resolve to its own server's HostName.
        var serverA = MakeServer("server-a");
        var serverB = MakeServer("server-b");
        var prodEnv = MakeEnv(EnvironmentType.Production, serverA);
        var previewEnv = MakeEnv(EnvironmentType.Preview, serverB);

        var prodDomain = new InternalDomain
        {
            ProjectId = ProjectId.CreateNew(),
            ProjectEnvironmentId = prodEnv.Id,
            ProjectEnvironment = prodEnv,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "prod-app",
        };

        var previewDomain = new InternalDomain
        {
            ProjectId = ProjectId.CreateNew(),
            ProjectEnvironmentId = previewEnv.Id,
            ProjectEnvironment = previewEnv,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "preview-app",
        };

        Assert.Equal("prod-app.sites.server-a.compozerr.com", prodDomain.Value);
        Assert.Equal("preview-app.sites.server-b.compozerr.com", previewDomain.Value);
    }

    [Fact]
    public void Value_FallsBackToProductionEnvServer_WhenDomainHasNoEnvScope()
    {
        // Apex / project-level domain (ProjectEnvironmentId is null) should
        // route to the production env's server.
        var prodServer = MakeServer("prod-server");
        var previewServer = MakeServer("preview-server");

        var projectId = ProjectId.CreateNew();
        var project = new Project
        {
            Id = projectId,
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = Auth.Abstractions.UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ProjectEnvironments = new List<ProjectEnvironment>
            {
                MakeEnv(EnvironmentType.Production, prodServer),
                MakeEnv(EnvironmentType.Preview, previewServer),
            },
        };

        var apexDomain = new InternalDomain
        {
            ProjectId = projectId,
            ProjectEnvironmentId = null,
            ProjectEnvironment = null,
            Project = project,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "apex",
        };

        Assert.Equal("apex.sites.prod-server.compozerr.com", apexDomain.Value);
    }

    [Fact]
    public void Value_FallsBackToLegacyProjectServer_WhenEnvsHaveNoServer()
    {
        // Transition path: envs haven't been backfilled and the only allocated
        // server is the project-level legacy hint. Must still produce a
        // non-empty hostname.
        var legacyServer = MakeServer("legacy-host");
        var projectId = ProjectId.CreateNew();
        var project = new Project
        {
            Id = projectId,
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = Auth.Abstractions.UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ServerId = legacyServer.Id,
            Server = legacyServer,
            ProjectEnvironments = new List<ProjectEnvironment>(),
        };

        var domain = new InternalDomain
        {
            ProjectId = projectId,
            ProjectEnvironmentId = null,
            Project = project,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "transition",
        };

        Assert.Equal("transition.sites.legacy-host.compozerr.com", domain.Value);
    }

    [Fact]
    public void Value_Throws_WhenHostnameCannotBeResolved()
    {
        // F-P2-1: no env server, no project, no legacy hint loaded. Previously
        // this silently produced "unresolved.sites..compozerr.com" (malformed).
        // It must now fail loudly so callers never pass a broken domain string
        // downstream.
        var domain = new InternalDomain
        {
            ProjectId = ProjectId.CreateNew(),
            ProjectEnvironmentId = null,
            ProjectEnvironment = null,
            Project = null,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "unresolved",
        };

        Assert.Throws<InvalidOperationException>(() => domain.Value);
    }
}
