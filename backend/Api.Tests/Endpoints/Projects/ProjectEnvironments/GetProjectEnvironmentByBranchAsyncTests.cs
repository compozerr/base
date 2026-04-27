using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.Feature;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.ProjectEnvironments;

/// <summary>
/// Regression tests for the branch-based environment lookup used by the deploy
/// pipeline (see <see cref="ProjectEnvironmentRepository.GetProjectEnvironmentByBranchAsync"/>).
///
/// Background: tag-triggered CI deploys send only a branch name to
/// <c>POST /v1/cli/projects/&#123;projectId&#125;/deployments</c>. When 2+ environments
/// list the same branch (e.g. a Production env and a Preview/staging env both
/// auto-deploying from <c>main</c>), the previous implementation called
/// <c>SingleOrDefaultAsync</c> and threw
/// "Sequence contains more than one element", causing every CI deploy to fail.
/// </summary>
public class GetProjectEnvironmentByBranchAsyncTests : IDisposable
{
    /// <summary>
    /// Test-only subclass of <see cref="ApiDbContext"/> that ignores entity
    /// properties unsupported by the InMemory provider (e.g. JSON-backed
    /// <see cref="Dictionary{TKey,TValue}"/>) so the model can be built without
    /// requiring Postgres-specific mappings.
    /// </summary>
    private sealed class TestApiDbContext(DbContextOptions<ApiDbContext> options, IMediator mediator)
        : ApiDbContext(options, mediator)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Postgres jsonb columns aren't natively understood by the
            // InMemory provider; ignore them for these repository tests.
            modelBuilder.Entity<ModuleRegistryEntry>().Ignore(e => e.Compatibility);
            modelBuilder.Entity<Server>().Ignore(e => e.Usage);
        }
    }

    private readonly ApiDbContext _context;
    private readonly ProjectEnvironmentRepository _repository;

    static GetProjectEnvironmentByBranchAsyncTests()
    {
        // BaseDbContext.OnModelCreating runs entity configuration over the
        // assemblies registered via AssembliesFeatureConfigureCallback. In a
        // production app these are populated by feature discovery at startup;
        // in tests we need to register them ourselves so strongly-typed Id
        // converters (UserId, ProjectId, etc.) get applied.
        var callback = (IFeatureConfigureCallback)new AssembliesFeatureConfigureCallback();
        callback.Configure(typeof(ProjectEnvironment));   // Api.Data assembly
        callback.Configure(typeof(ApiDbContext));         // Api.Data assembly (idempotent)
    }

    public GetProjectEnvironmentByBranchAsyncTests()
    {
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: $"branch-lookup-{Guid.NewGuid()}")
            .Options;

        _context = new TestApiDbContext(options, Substitute.For<IMediator>());
        _repository = new ProjectEnvironmentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task ReturnsProductionEnv_WhenMultipleEnvsShareBranch()
    {
        // Reproduces the kurvo prod scenario: a Production env (Type=0) and a
        // Preview/staging env (Type=1) both list "main" in their Branches.
        var projectId = ProjectId.CreateNew();
        var prod = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };
        var staging = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "staging",
        };

        _context.ProjectEnvironments.AddRange(prod, staging);
        await _context.SaveChangesAsync();
        await SetCreatedAtUtcAsync(prod, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SetCreatedAtUtcAsync(staging, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.GetProjectEnvironmentByBranchAsync(projectId, "main");

        Assert.NotNull(result);
        Assert.Equal(prod.Id, result!.Id);
        Assert.Equal(EnvironmentType.Production, result.Type);
    }

    [Fact]
    public async Task ReturnsMostRecentlyCreated_WhenSameTypeEnvsShareBranch()
    {
        // When two envs of the same Type share a branch, prefer the most
        // recently created one (deterministic tie-breaker).
        var projectId = ProjectId.CreateNew();
        var older = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "develop" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "preview-old",
        };
        var newer = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "develop" },
            AutoDeploy = true,
            Type = EnvironmentType.Preview,
            Name = "preview-new",
        };

        _context.ProjectEnvironments.AddRange(older, newer);
        await _context.SaveChangesAsync();
        await SetCreatedAtUtcAsync(older, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await SetCreatedAtUtcAsync(newer, new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await _repository.GetProjectEnvironmentByBranchAsync(projectId, "develop");

        Assert.NotNull(result);
        Assert.Equal(newer.Id, result!.Id);
    }

    /// <summary>
    /// <see cref="BaseDbContext{T}.SaveChangesAsync"/> overrides <see cref="ProjectEnvironment.CreatedAtUtc"/>
    /// for entries in the <c>Added</c> state, so we set it after the initial save
    /// (when the entry is in the <c>Modified</c> state) to control ordering in tests.
    /// </summary>
    private async Task SetCreatedAtUtcAsync(ProjectEnvironment env, DateTime createdAtUtc)
    {
        env.CreatedAtUtc = createdAtUtc;
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ReturnsSingleMatch_Unchanged()
    {
        // Single-match case must keep the prior behavior of returning that env.
        var projectId = ProjectId.CreateNew();
        var prod = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };

        _context.ProjectEnvironments.Add(prod);
        await _context.SaveChangesAsync();

        var result = await _repository.GetProjectEnvironmentByBranchAsync(projectId, "main");

        Assert.NotNull(result);
        Assert.Equal(prod.Id, result!.Id);
    }

    [Fact]
    public async Task ReturnsNull_WhenNoEnvMatchesBranch()
    {
        // Zero-match case must still return null so callers (e.g.
        // DeployProjectCommandHandler) can fall back to the production env.
        var projectId = ProjectId.CreateNew();
        var prod = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectId,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };

        _context.ProjectEnvironments.Add(prod);
        await _context.SaveChangesAsync();

        var result = await _repository.GetProjectEnvironmentByBranchAsync(projectId, "nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task DoesNotReturnEnvFromDifferentProject()
    {
        // A matching branch on a *different* project must not leak through.
        var projectA = ProjectId.CreateNew();
        var projectB = ProjectId.CreateNew();

        var envOnA = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectA,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };
        var envOnB = new ProjectEnvironment
        {
            Id = ProjectEnvironmentId.CreateNew(),
            ProjectId = projectB,
            Branches = new List<string> { "main" },
            AutoDeploy = true,
            Type = EnvironmentType.Production,
        };

        _context.ProjectEnvironments.AddRange(envOnA, envOnB);
        await _context.SaveChangesAsync();

        var result = await _repository.GetProjectEnvironmentByBranchAsync(projectA, "main");

        Assert.NotNull(result);
        Assert.Equal(envOnA.Id, result!.Id);
    }
}
