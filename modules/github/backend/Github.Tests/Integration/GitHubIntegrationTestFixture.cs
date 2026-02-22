using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Database.Data;
using Github.Data;
using Github.Repositories;
using Github.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Octokit;
using Testcontainers.PostgreSql;

namespace Github.Tests.Integration;

public class GitHubIntegrationTestFixture : IAsyncLifetime
{
    private const string ProjectRepoOwner = "compozerr-test";
    private const string ProjectRepoName = "integration-test-project";
    private const string ModuleRepoOwner = "compozerr-test";
    private const string ModuleRepoName = "integration-test-module";

    private PostgreSqlContainer? _postgresContainer;

    public bool IsConfigured { get; private set; }
    public IGitHubClient? GitHubClient { get; private set; }
    public string? ProjectBaselineSha { get; private set; }
    public string? ModuleBaselineSha { get; private set; }
    public IServiceProvider? Services { get; private set; }

    public string RepoOwner => ProjectRepoOwner;
    public string RepoName => ProjectRepoName;
    public string ModRepoOwner => ModuleRepoOwner;
    public string ModRepoName => ModuleRepoName;

    public async Task InitializeAsync()
    {
        var appId = Environment.GetEnvironmentVariable("GITHUB__GITHUBAPP__APP_ID");
        var privateKeyBase64 = Environment.GetEnvironmentVariable("GITHUB__GITHUBAPP__PRIVATE_KEY_CERTIFICATE_BASE64");
        var installationId = Environment.GetEnvironmentVariable("INTEGRATION_TEST_INSTALLATION_ID");

        if (string.IsNullOrEmpty(appId) ||
            string.IsNullOrEmpty(privateKeyBase64) ||
            string.IsNullOrEmpty(installationId))
        {
            IsConfigured = false;
            return;
        }

        IsConfigured = true;

        // Create GitHub installation client
        GitHubClient = await GitHubTestHelper.CreateInstallationClientAsync(
            appId, privateKeyBase64, installationId);

        // Record baseline SHAs
        ProjectBaselineSha = await GitHubTestHelper.GetMainBranchShaAsync(
            GitHubClient, ProjectRepoOwner, ProjectRepoName);
        ModuleBaselineSha = await GitHubTestHelper.GetMainBranchShaAsync(
            GitHubClient, ModuleRepoOwner, ModuleRepoName);

        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgresContainer.StartAsync();

        // Build service provider
        Services = BuildServiceProvider(_postgresContainer.GetConnectionString());
    }

    private static IServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        services.AddDbContext<GithubDbContext>(options =>
        {
            options.UseNpgsql(dataSource, b =>
                b.MigrationsAssembly(typeof(GithubDbContext).Assembly.FullName));
        });

        services.AddDbContext<ApiDbContext>(options =>
        {
            options.UseNpgsql(dataSource, b =>
                b.MigrationsAssembly(typeof(ApiDbContext).Assembly.FullName));
        });

        // Mock MediatR (no-op)
        var mockMediator = new Mock<IMediator>();
        services.AddSingleton(mockMediator.Object);

        // Mock ICurrentUserAccessor (no-op)
        var mockCurrentUser = new Mock<ICurrentUserAccessor>();
        services.AddSingleton(mockCurrentUser.Object);

        // Repositories
        services.AddScoped<IPushWebhookEventRepository, PushWebhookEventRepository>();
        services.AddScoped<IModuleSyncEventRepository, ModuleSyncEventRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();

        // Services
        services.AddScoped<IModuleSyncService, ModuleSyncService>();

        // IServiceScopeFactory is automatically registered by the DI container
        var sp = services.BuildServiceProvider();

        // Run migrations
        using var scope = sp.CreateScope();
        var githubDb = scope.ServiceProvider.GetRequiredService<GithubDbContext>();
        githubDb.Database.Migrate();

        var apiDb = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
        apiDb.Database.Migrate();

        return sp;
    }

    public async Task ResetReposAsync()
    {
        if (!IsConfigured || GitHubClient is null) return;

        if (ProjectBaselineSha is not null)
        {
            await GitHubTestHelper.ResetMainBranchAsync(
                GitHubClient, ProjectRepoOwner, ProjectRepoName, ProjectBaselineSha);
        }

        if (ModuleBaselineSha is not null)
        {
            await GitHubTestHelper.ResetMainBranchAsync(
                GitHubClient, ModuleRepoOwner, ModuleRepoName, ModuleBaselineSha);
        }
    }

    public async Task DisposeAsync()
    {
        await ResetReposAsync();

        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}
