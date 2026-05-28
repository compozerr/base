using Api.Abstractions;
using Api.Data;
using Api.Endpoints.Projects.Project.Start;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Auth.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.ProjectStart;

/// <summary>
/// F-P2-2: project-wide Start fans out across every env. Preview env VMs are
/// keyed by env name in the VM identifier (HostingApi.BuildVmIdentifier), so the
/// handler must pass each preview env's Name through to hosting — production
/// gets a bare-project start (null name). The prior code passed
/// <c>command.EnvironmentName</c> (null on a project-wide start) for every env,
/// so preview env VMs were never started.
/// </summary>
public class StartProjectCommandHandlerTests : IDisposable
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

    static StartProjectCommandHandlerTests() => ApiDbContextTestBootstrap.EnsureInitialized();

    private readonly ApiDbContext _context;
    private readonly IEnvironmentTargetResolver _resolver = Substitute.For<IEnvironmentTargetResolver>();

    public StartProjectCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"start-project-{Guid.NewGuid()}")
            .Options;
        _context = new TestApiDbContext(options, Substitute.For<IMediator>());
    }

    public void Dispose() => _context.Dispose();

    private static Server NewServer() => new()
    {
        Id = ServerId.CreateNew(),
        SecretId = SecretId.CreateNew(),
        HostName = "h",
        ApiDomain = "api.h.example",
        Ip = "10.0.0.1",
        ServerVisibility = ServerVisibility.Public,
    };

    [Fact]
    public async Task ProjectWideStart_StartsProductionByProjectId_AndPreviewByEnvName()
    {
        var projectId = ProjectId.CreateNew();
        var serverA = NewServer();
        var serverB = NewServer();

        var project = new Data.Project
        {
            Id = projectId,
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
        };
        _context.Projects.Add(project);

        var prodEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Type = EnvironmentType.Production,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            ServerId = serverA.Id,
        };
        var previewEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Type = EnvironmentType.Preview,
            Name = "feature-x",
            Branches = new List<string> { "feature-x" },
            AutoDeploy = true,
            ServerId = serverB.Id,
        };
        _context.ProjectEnvironments.AddRange(prodEnv, previewEnv);
        await _context.SaveChangesAsync();

        var apiA = Substitute.For<IHostingApi>();
        var apiB = Substitute.For<IHostingApi>();

        _resolver.TryGetServerAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(serverA);
        _resolver.TryGetServerAsync(previewEnv.Id, Arg.Any<CancellationToken>()).Returns(serverB);
        _resolver.GetHostingApiAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(apiA);
        _resolver.GetHostingApiAsync(previewEnv.Id, Arg.Any<CancellationToken>()).Returns(apiB);

        var handler = new StartProjectCommandHandler(_context, _resolver);
        await handler.Handle(new StartProjectCommand(projectId));

        // Server A (production): started by bare project id (null env name).
        await apiA.Received(1).StartProjectAsync(projectId, null);
        // Server B (preview): started with the preview env's name so the VM
        // identifier resolves to the preview VM.
        await apiB.Received(1).StartProjectAsync(projectId, "feature-x");
    }

    [Fact]
    public async Task Start_SkipsEnvsWithoutAResolvableServer()
    {
        var projectId = ProjectId.CreateNew();
        var serverA = NewServer();

        var project = new Data.Project
        {
            Id = projectId,
            Name = "p",
            RepoUri = new Uri("https://github.com/test/repo"),
            UserId = UserId.CreateNew(),
            LocationId = LocationId.CreateNew(),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("T0"),
        };
        _context.Projects.Add(project);

        var prodEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Type = EnvironmentType.Production,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            ServerId = serverA.Id,
        };
        var unallocatedPreview = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Type = EnvironmentType.Preview,
            Name = "feature-y",
            Branches = new List<string> { "feature-y" },
            AutoDeploy = true,
            ServerId = null,
        };
        _context.ProjectEnvironments.AddRange(prodEnv, unallocatedPreview);
        await _context.SaveChangesAsync();

        var apiA = Substitute.For<IHostingApi>();
        _resolver.TryGetServerAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(serverA);
        _resolver.TryGetServerAsync(unallocatedPreview.Id, Arg.Any<CancellationToken>()).Returns((Server?)null);
        _resolver.GetHostingApiAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(apiA);

        var handler = new StartProjectCommandHandler(_context, _resolver);
        await handler.Handle(new StartProjectCommand(projectId));

        await apiA.Received(1).StartProjectAsync(projectId, null);
        // The env with no resolvable server must not produce a hosting call.
        await _resolver.DidNotReceive().GetHostingApiAsync(unallocatedPreview.Id, Arg.Any<CancellationToken>());
    }
}
