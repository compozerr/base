using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Api.Tests.Services;

/// <summary>
/// Integration coverage for <see cref="EnvironmentTargetResolver"/>: the seam
/// that turns a ProjectEnvironmentId into the server/api that should serve it.
/// Post-Spec-B these reads spread across ~30 call sites so the resolver's
/// behaviour is load-bearing.
/// </summary>
public class EnvironmentTargetResolverTests : IDisposable
{
    private sealed class TestApiDbContext(DbContextOptions<ApiDbContext> options, IMediator mediator)
        : ApiDbContext(options, mediator)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ModuleRegistryEntry>().Ignore(e => e.Compatibility);
            modelBuilder.Entity<Server>().Ignore(e => e.Usage);
        }
    }

    static EnvironmentTargetResolverTests() => ApiDbContextTestBootstrap.EnsureInitialized();

    private readonly ApiDbContext _context;
    private readonly IHostingApiFactory _hostingApiFactory;
    private readonly EnvironmentTargetResolver _resolver;

    public EnvironmentTargetResolverTests()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"env-target-resolver-{Guid.NewGuid()}")
            .Options;

        _context = new TestApiDbContext(options, Substitute.For<IMediator>());
        _hostingApiFactory = Substitute.For<IHostingApiFactory>();
        _resolver = new EnvironmentTargetResolver(_context, _hostingApiFactory);
    }

    public void Dispose() => _context.Dispose();

    private async Task<(Project project, Server serverA, Server serverB, ProjectEnvironment prodEnv, ProjectEnvironment previewEnv)> SeedTwoEnvsTwoServersAsync()
    {
        var serverA = new Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            HostName = "server-a",
            ApiDomain = "api.server-a.example",
            Ip = "10.0.0.1",
            ServerVisibility = Api.Data.ServerVisibility.Public,
        };
        var serverB = new Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            HostName = "server-b",
            ApiDomain = "api.server-b.example",
            Ip = "10.0.0.2",
            ServerVisibility = Api.Data.ServerVisibility.Public,
        };

        _context.Servers.AddRange(serverA, serverB);

        var project = new Project
        {
            Id = ProjectId.CreateNew(),
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = Auth.Abstractions.UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ServerId = serverA.Id,
        };
        _context.Projects.Add(project);

        var prodEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = project.Id,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
            ServerId = serverA.Id,
        };
        var previewEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = project.Id,
            Branches = new List<string> { "preview" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "preview",
            ServerId = serverB.Id,
        };
        _context.ProjectEnvironments.AddRange(prodEnv, previewEnv);

        await _context.SaveChangesAsync();
        return (project, serverA, serverB, prodEnv, previewEnv);
    }

    [Fact]
    public async Task GetServerAsync_ReturnsEnvOwnServer_WhenEnvHasServerId()
    {
        var (_, serverA, serverB, prodEnv, previewEnv) = await SeedTwoEnvsTwoServersAsync();

        var prodServer = await _resolver.GetServerAsync(prodEnv.Id);
        var previewServer = await _resolver.GetServerAsync(previewEnv.Id);

        Assert.Equal(serverA.Id, prodServer.Id);
        Assert.Equal(serverB.Id, previewServer.Id);
    }

    [Fact]
    public async Task GetServerAsync_FallsBackToProjectHint_WhenEnvHasNoServerId()
    {
        // Transition-period env: env.ServerId is null but the project still
        // carries the legacy hint. Resolver must surface the hint so the
        // legacy code paths keep working until backfill completes.
        var legacy = new Server
        {
            Id = ServerId.CreateNew(),
            SecretId = SecretId.CreateNew(),
            HostName = "legacy",
            ApiDomain = "api.legacy.example",
            Ip = "10.0.0.9",
            ServerVisibility = Api.Data.ServerVisibility.Public,
        };
        _context.Servers.Add(legacy);
        var project = new Project
        {
            Id = ProjectId.CreateNew(),
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = Auth.Abstractions.UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ServerId = legacy.Id,
        };
        _context.Projects.Add(project);
        var env = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = project.Id,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
            ServerId = null,
        };
        _context.ProjectEnvironments.Add(env);
        await _context.SaveChangesAsync();

        var server = await _resolver.GetServerAsync(env.Id);
        Assert.Equal(legacy.Id, server.Id);
    }

    [Fact]
    public async Task GetServerForDomainAsync_UsesEnvServer_WhenDomainIsEnvScoped()
    {
        var (project, _, serverB, _, previewEnv) = await SeedTwoEnvsTwoServersAsync();

        var domain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = project.Id,
            ProjectEnvironmentId = previewEnv.Id,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "preview-app",
        };
        _context.Domains.Add(domain);
        await _context.SaveChangesAsync();

        var server = await _resolver.GetServerForDomainAsync(domain.Id);
        Assert.Equal(serverB.Id, server.Id);
    }

    [Fact]
    public async Task GetServerForDomainAsync_RejectsCrossProjectEnvironment()
    {
        // F-P1-1: a domain belongs to project A, but its ProjectEnvironmentId
        // points at an env owned by project B (cross-project hijack attempt).
        // The resolver must NOT route project A's domain to project B's server —
        // it must fail loudly instead.
        var (projectA, _, serverB, _, previewEnvOfB) = await SeedTwoEnvsTwoServersAsync();

        // previewEnvOfB currently belongs to projectA; move it to a brand-new
        // project B (on serverB) to set up the cross-project reference.
        var projectB = new Project
        {
            Id = ProjectId.CreateNew(),
            Name = "b",
            RepoUri = new Uri("https://github.com/test/repo-b"),
            UserId = Auth.Abstractions.UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
            ServerId = serverB.Id,
        };
        _context.Projects.Add(projectB);

        var envOfB = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectB.Id,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
            ServerId = serverB.Id,
        };
        _context.ProjectEnvironments.Add(envOfB);

        // Domain owned by project A, but pinned to project B's env.
        var hijackDomain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = projectA.Id,
            ProjectEnvironmentId = envOfB.Id,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "hijack",
        };
        _context.Domains.Add(hijackDomain);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _resolver.GetServerForDomainAsync(hijackDomain.Id));
    }

    [Fact]
    public async Task GetServerForDomainAsync_FallsBackToProductionEnvServer_ForApexDomain()
    {
        // Apex / project-level domain (no ProjectEnvironmentId) must resolve
        // to the production env's server.
        var (project, serverA, _, _, _) = await SeedTwoEnvsTwoServersAsync();

        var apexDomain = new InternalDomain
        {
            Id = DomainId.CreateNew(),
            ProjectId = project.Id,
            ProjectEnvironmentId = null,
            ServiceName = "Web",
            Port = "3000",
            Protocol = Protocol.Http,
            Subdomain = "apex",
        };
        _context.Domains.Add(apexDomain);
        await _context.SaveChangesAsync();

        var server = await _resolver.GetServerForDomainAsync(apexDomain.Id);
        Assert.Equal(serverA.Id, server.Id);
    }

    [Fact]
    public async Task GetDistinctServersForProjectAsync_DeduplicatesAndIncludesProjectHint()
    {
        var (project, serverA, serverB, _, _) = await SeedTwoEnvsTwoServersAsync();

        // A third env on serverA — should not duplicate serverA in the result.
        var extra = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = project.Id,
            Branches = new List<string> { "extra" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "extra",
            ServerId = serverA.Id,
        };
        _context.ProjectEnvironments.Add(extra);
        await _context.SaveChangesAsync();

        var servers = await _resolver.GetDistinctServersForProjectAsync(project.Id);
        var ids = servers.Select(s => s.Id).ToHashSet();
        Assert.Contains(serverA.Id, ids);
        Assert.Contains(serverB.Id, ids);
        Assert.Equal(2, servers.Count);
    }

    [Fact]
    public async Task GetDistinctHostingApisForProjectAsync_FansOutOverAllEnvServers()
    {
        // Regression for UpdateDomains_DomainChangeEventHandler fan-out: when
        // a project's envs span multiple servers, every server must get a
        // hosting-api instance back — one per distinct server, not one per
        // env (we don't want N pushes when two envs share a server).
        var (project, serverA, serverB, _, _) = await SeedTwoEnvsTwoServersAsync();

        var apiA = Substitute.For<IHostingApi>();
        var apiB = Substitute.For<IHostingApi>();
        _hostingApiFactory.GetHostingApiAsync(serverA.Id).Returns(apiA);
        _hostingApiFactory.GetHostingApiAsync(serverB.Id).Returns(apiB);

        var apis = await _resolver.GetDistinctHostingApisForProjectAsync(project.Id);

        Assert.Equal(2, apis.Count);
        Assert.Contains(apiA, apis);
        Assert.Contains(apiB, apis);
    }
}
