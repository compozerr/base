using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Project.Delete;
using Api.Hosting.Services;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Auth.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Stripe.Services;

namespace Api.Tests.Endpoints.Projects.ProjectDelete;

/// <summary>
/// F-P1-2: <see cref="DeleteProjectCommandHandler"/> must tear down EACH env on
/// the env's own server with the correct hosting call. Preview env VMs use a
/// distinct identifier and require <see cref="IHostingApi.DeletePreviewEnvironmentAsync"/>;
/// the prior fan-out called <see cref="IHostingApi.DeleteProjectAsync"/> on every
/// distinct server, orphaning preview VMs on non-prod servers. A teardown failure
/// must abort BEFORE the DB row is deleted.
/// </summary>
public class DeleteProjectCommandHandlerTests : IDisposable
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

    static DeleteProjectCommandHandlerTests() => ApiDbContextTestBootstrap.EnsureInitialized();

    private readonly ApiDbContext _context;
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IEnvironmentTargetResolver _resolver = Substitute.For<IEnvironmentTargetResolver>();
    private readonly IProjectEnvironmentRepository _environmentRepository = Substitute.For<IProjectEnvironmentRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ISubscriptionsService _subscriptionsService = Substitute.For<ISubscriptionsService>();
    private readonly IDeploymentRepository _deploymentRepository = Substitute.For<IDeploymentRepository>();

    public DeleteProjectCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"delete-project-{Guid.NewGuid()}")
            .Options;
        _context = new TestApiDbContext(options, Substitute.For<IMediator>());

        // The handler queries deployments via an EF async IQueryable; back it with
        // a real (empty) in-memory DbSet so ToListAsync works.
        _deploymentRepository.GetDeploymentsForProject(Arg.Any<ProjectId>())
            .Returns(ci => _context.Deployments.Where(d => d.ProjectId == ci.Arg<ProjectId>()));
    }

    public void Dispose() => _context.Dispose();

    private DeleteProjectCommandHandler NewHandler() => new(
        _projectRepository,
        _resolver,
        _environmentRepository,
        _sender,
        _subscriptionsService,
        _deploymentRepository);

    private static Server NewServer() => new()
    {
        Id = ServerId.CreateNew(),
        SecretId = SecretId.CreateNew(),
        ServerVisibility = ServerVisibility.Public,
    };

    private static Data.Project NewProject(ProjectId id) => new()
    {
        Id = id,
        Name = "p",
        RepoUri = new Uri("https://github.com/test/repo"),
        UserId = UserId.CreateNew(),
        LocationId = LocationId.CreateNew(),
        State = ProjectState.Running,
        ServerTierId = new ServerTierId("T0"),
    };

    [Fact]
    public async Task PreviewEnvFanout_TearsDownEachEnvOnItsOwnServerWithCorrectCall()
    {
        var projectId = ProjectId.CreateNew();
        var serverA = NewServer();
        var serverB = NewServer();

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
            Name = "preview",
            Branches = new List<string> { "feature" },
            AutoDeploy = true,
            ServerId = serverB.Id,
        };

        var apiA = Substitute.For<IHostingApi>();
        var apiB = Substitute.For<IHostingApi>();

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(NewProject(projectId));
        _environmentRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<ProjectEnvironment> { prodEnv, previewEnv });

        _resolver.TryGetServerAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(serverA);
        _resolver.TryGetServerAsync(previewEnv.Id, Arg.Any<CancellationToken>()).Returns(serverB);
        _resolver.GetHostingApiAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(apiA);
        _resolver.GetHostingApiAsync(previewEnv.Id, Arg.Any<CancellationToken>()).Returns(apiB);

        await NewHandler().Handle(new DeleteProjectCommand(projectId));

        // Production env: whole-project teardown on its server.
        await apiA.Received(1).DeleteProjectAsync(projectId);
        await apiA.DidNotReceiveWithAnyArgs().DeletePreviewEnvironmentAsync(default!, default!);

        // Preview env: preview-scoped teardown on ITS server, with the env name.
        await apiB.Received(1).DeletePreviewEnvironmentAsync(projectId, "preview");
        await apiB.DidNotReceiveWithAnyArgs().DeleteProjectAsync(default!);

        // DB row deleted on success.
        await _projectRepository.Received(1).DeleteAsync(projectId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewEnvFanout_AbortsWithoutDeletingDbRow_WhenTeardownFails()
    {
        var projectId = ProjectId.CreateNew();
        var serverA = NewServer();

        var prodEnv = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Type = EnvironmentType.Production,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            ServerId = serverA.Id,
        };

        var apiA = Substitute.For<IHostingApi>();
        apiA.DeleteProjectAsync(projectId).Returns(_ => throw new InvalidOperationException("hosting down"));

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(NewProject(projectId));
        _environmentRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<ProjectEnvironment> { prodEnv });
        _resolver.TryGetServerAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(serverA);
        _resolver.GetHostingApiAsync(prodEnv.Id, Arg.Any<CancellationToken>()).Returns(apiA);

        await Assert.ThrowsAsync<AggregateException>(() =>
            NewHandler().Handle(new DeleteProjectCommand(projectId)));

        // The project row must NOT be deleted — the user can retry.
        await _projectRepository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
