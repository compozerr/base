using System.Text.Json;
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Abstractions;
using Github.Abstractions;
using Github.Data;
using Github.Jobs;
using Github.Models;
using Github.Repositories;
using Github.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Octokit.Webhooks.Events;

namespace Github.Tests.Integration;

[Collection("GitHubIntegration")]
[Trait("Category", "Integration")]
public class ModuleSyncProcessorJobIntegrationTests : IAsyncLifetime
{
    private readonly GitHubIntegrationTestFixture _fixture;

    public ModuleSyncProcessorJobIntegrationTests(GitHubIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.CleanupDatabaseAsync();

    public async Task DisposeAsync()
    {
        await _fixture.ResetReposAsync();
    }

    [SkippableFact]
    public async Task ExecuteAsync_WithModuleChanges_CreatesModuleSyncEvent()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        using var scope = _fixture.Services!.CreateScope();

        // Create a commit with module files
        var (beforeSha, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/SyncedFile.cs"] = "public class SyncedFile { }"
            });

        // Seed DB: Project
        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var project = await projectRepo.AddAsync(new Project
        {
            Name = "integration-test-project",
            RepoUri = new Uri($"https://github.com/{_fixture.RepoOwner}/{_fixture.RepoName}.git"),
            UserId = UserId.Create(Guid.NewGuid()),
            LocationId = LocationId.Create(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("STARTER"),
            GithubInstallationId = long.Parse(
                Environment.GetEnvironmentVariable("INTEGRATION_TEST_INSTALLATION_ID")!)
        });

        // Seed DB: Module
        var moduleRepo = scope.ServiceProvider.GetRequiredService<IModuleRepository>();
        await moduleRepo.AddAsync(new Module
        {
            Name = "test-module",
            RepoUri = new Uri($"https://github.com/{_fixture.ModRepoOwner}/{_fixture.ModRepoName}")
        });

        // Create PushWebhookEvent
        var pushEvent = CreatePushEvent(
            _fixture.RepoOwner, _fixture.RepoName,
            beforeSha, afterSha,
            "refs/heads/main",
            "test: add synced file");

        var pushEventRepo = scope.ServiceProvider.GetRequiredService<IPushWebhookEventRepository>();
        var pushWebhookEvent = await pushEventRepo.AddAsync(new PushWebhookEvent
        {
            Event = pushEvent
        });

        // Create mock IGithubService that returns real client
        var mockGithubService = new Mock<IGithubService>();
        mockGithubService
            .Setup(s => s.GetInstallationClientByInstallationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetInstallationClientByInstallationIdResponse(client, "test-token"));

        // Build and execute the job
        var moduleSyncService = scope.ServiceProvider.GetRequiredService<IModuleSyncService>();
        var moduleSyncEventRepo = scope.ServiceProvider.GetRequiredService<IModuleSyncEventRepository>();

        var job = new ModuleSyncProcessorJob(
            pushEventRepo,
            moduleSyncEventRepo,
            mockGithubService.Object,
            moduleSyncService,
            _fixture.Services as IServiceScopeFactory
                ?? scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        await job.ExecuteAsync(pushWebhookEvent.Id);

        // Assert: ModuleSyncEvent was created with Succeeded status
        var syncEvents = await moduleSyncEventRepo.GetByPushWebhookEventIdAsync(pushWebhookEvent.Id);
        Assert.Single(syncEvents);
        Assert.Equal("test-module", syncEvents[0].ModuleName);
        Assert.Equal(ModuleSyncStatus.Succeeded, syncEvents[0].Status);
        Assert.NotNull(syncEvents[0].TargetCommitHash);

        // Verify file arrived in module repo
        var content = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName,
            "backend/SyncedFile.cs", syncEvents[0].TargetCommitHash);
        Assert.NotNull(content);
        Assert.Contains("SyncedFile", content);
    }

    [SkippableFact]
    public async Task ExecuteAsync_SkipsSyncCommits_PreventsLoops()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        using var scope = _fixture.Services!.CreateScope();

        // Create push event with "sync:" prefix in commit message
        var pushEvent = CreatePushEvent(
            _fixture.RepoOwner, _fixture.RepoName,
            "abc123", "def456",
            "refs/heads/main",
            "sync: synced from other repo");

        var pushEventRepo = scope.ServiceProvider.GetRequiredService<IPushWebhookEventRepository>();
        var pushWebhookEvent = await pushEventRepo.AddAsync(new PushWebhookEvent
        {
            Event = pushEvent
        });

        // Seed project so it's found
        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        await projectRepo.AddAsync(new Project
        {
            Name = "loop-test-project",
            RepoUri = new Uri($"https://github.com/{_fixture.RepoOwner}/{_fixture.RepoName}.git"),
            UserId = UserId.Create(Guid.NewGuid()),
            LocationId = LocationId.Create(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("STARTER"),
            GithubInstallationId = 12345
        });

        var mockGithubService = new Mock<IGithubService>();
        var moduleSyncService = scope.ServiceProvider.GetRequiredService<IModuleSyncService>();
        var moduleSyncEventRepo = scope.ServiceProvider.GetRequiredService<IModuleSyncEventRepository>();

        var job = new ModuleSyncProcessorJob(
            pushEventRepo,
            moduleSyncEventRepo,
            mockGithubService.Object,
            moduleSyncService,
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        await job.ExecuteAsync(pushWebhookEvent.Id);

        // Assert: no sync events created
        var syncEvents = await moduleSyncEventRepo.GetByPushWebhookEventIdAsync(pushWebhookEvent.Id);
        Assert.Empty(syncEvents);
    }

    [SkippableFact]
    public async Task ExecuteAsync_SkipSyncTag_SkipsProcessing()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        using var scope = _fixture.Services!.CreateScope();

        var pushEvent = CreatePushEvent(
            _fixture.RepoOwner, _fixture.RepoName,
            "abc123", "def456",
            "refs/heads/main",
            "feat: add feature [skip sync]");

        var pushEventRepo = scope.ServiceProvider.GetRequiredService<IPushWebhookEventRepository>();
        var pushWebhookEvent = await pushEventRepo.AddAsync(new PushWebhookEvent
        {
            Event = pushEvent
        });

        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        await projectRepo.AddAsync(new Project
        {
            Name = "skip-sync-test",
            RepoUri = new Uri($"https://github.com/{_fixture.RepoOwner}/{_fixture.RepoName}.git"),
            UserId = UserId.Create(Guid.NewGuid()),
            LocationId = LocationId.Create(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("STARTER"),
            GithubInstallationId = 12345
        });

        var mockGithubService = new Mock<IGithubService>();
        var moduleSyncService = scope.ServiceProvider.GetRequiredService<IModuleSyncService>();
        var moduleSyncEventRepo = scope.ServiceProvider.GetRequiredService<IModuleSyncEventRepository>();

        var job = new ModuleSyncProcessorJob(
            pushEventRepo,
            moduleSyncEventRepo,
            mockGithubService.Object,
            moduleSyncService,
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        await job.ExecuteAsync(pushWebhookEvent.Id);

        var syncEvents = await moduleSyncEventRepo.GetByPushWebhookEventIdAsync(pushWebhookEvent.Id);
        Assert.Empty(syncEvents);
    }

    [SkippableFact]
    public async Task ExecuteAsync_NonMainBranch_SkipsProcessing()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        using var scope = _fixture.Services!.CreateScope();

        var pushEvent = CreatePushEvent(
            _fixture.RepoOwner, _fixture.RepoName,
            "abc123", "def456",
            "refs/heads/feature/foo",
            "feat: work on feature branch");

        var pushEventRepo = scope.ServiceProvider.GetRequiredService<IPushWebhookEventRepository>();
        var pushWebhookEvent = await pushEventRepo.AddAsync(new PushWebhookEvent
        {
            Event = pushEvent
        });

        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        await projectRepo.AddAsync(new Project
        {
            Name = "branch-test",
            RepoUri = new Uri($"https://github.com/{_fixture.RepoOwner}/{_fixture.RepoName}.git"),
            UserId = UserId.Create(Guid.NewGuid()),
            LocationId = LocationId.Create(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("STARTER"),
            GithubInstallationId = 12345
        });

        var mockGithubService = new Mock<IGithubService>();
        var moduleSyncService = scope.ServiceProvider.GetRequiredService<IModuleSyncService>();
        var moduleSyncEventRepo = scope.ServiceProvider.GetRequiredService<IModuleSyncEventRepository>();

        var job = new ModuleSyncProcessorJob(
            pushEventRepo,
            moduleSyncEventRepo,
            mockGithubService.Object,
            moduleSyncService,
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        await job.ExecuteAsync(pushWebhookEvent.Id);

        var syncEvents = await moduleSyncEventRepo.GetByPushWebhookEventIdAsync(pushWebhookEvent.Id);
        Assert.Empty(syncEvents);
    }

    [SkippableFact]
    public async Task ExecuteAsync_UnregisteredModule_CreatesSkippedSyncEvent()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        using var scope = _fixture.Services!.CreateScope();

        // Create a commit with files in an unregistered module
        var (beforeSha, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/unregistered-module/backend/File.cs"] = "public class File { }"
            });

        var projectRepo = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
        var project = await projectRepo.AddAsync(new Project
        {
            Name = "unregistered-module-test",
            RepoUri = new Uri($"https://github.com/{_fixture.RepoOwner}/{_fixture.RepoName}.git"),
            UserId = UserId.Create(Guid.NewGuid()),
            LocationId = LocationId.Create(Guid.NewGuid()),
            State = ProjectState.Running,
            ServerTierId = new ServerTierId("STARTER"),
            GithubInstallationId = long.Parse(
                Environment.GetEnvironmentVariable("INTEGRATION_TEST_INSTALLATION_ID")!)
        });

        var pushEvent = CreatePushEvent(
            _fixture.RepoOwner, _fixture.RepoName,
            beforeSha, afterSha,
            "refs/heads/main",
            "feat: add unregistered module files");

        var pushEventRepo = scope.ServiceProvider.GetRequiredService<IPushWebhookEventRepository>();
        var pushWebhookEvent = await pushEventRepo.AddAsync(new PushWebhookEvent
        {
            Event = pushEvent
        });

        var mockGithubService = new Mock<IGithubService>();
        mockGithubService
            .Setup(s => s.GetInstallationClientByInstallationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetInstallationClientByInstallationIdResponse(client, "test-token"));

        var moduleSyncService = scope.ServiceProvider.GetRequiredService<IModuleSyncService>();
        var moduleSyncEventRepo = scope.ServiceProvider.GetRequiredService<IModuleSyncEventRepository>();

        var job = new ModuleSyncProcessorJob(
            pushEventRepo,
            moduleSyncEventRepo,
            mockGithubService.Object,
            moduleSyncService,
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>());

        await job.ExecuteAsync(pushWebhookEvent.Id);

        // Assert: ModuleSyncEvent with Skipped status
        var syncEvents = await moduleSyncEventRepo.GetByPushWebhookEventIdAsync(pushWebhookEvent.Id);
        Assert.Single(syncEvents);
        Assert.Equal("unregistered-module", syncEvents[0].ModuleName);
        Assert.Equal(ModuleSyncStatus.Skipped, syncEvents[0].Status);
    }

    private static PushEvent CreatePushEvent(
        string owner, string repo,
        string beforeSha, string afterSha,
        string @ref, string commitMessage)
    {
        // PushEvent from Octokit.Webhooks uses System.Text.Json deserialization
        var json = $$"""
        {
            "ref": "{{@ref}}",
            "before": "{{beforeSha}}",
            "after": "{{afterSha}}",
            "repository": {
                "id": 1,
                "name": "{{repo}}",
                "full_name": "{{owner}}/{{repo}}",
                "clone_url": "https://github.com/{{owner}}/{{repo}}.git",
                "owner": {
                    "login": "{{owner}}",
                    "id": 1
                }
            },
            "head_commit": {
                "id": "{{afterSha}}",
                "message": "{{commitMessage}}",
                "author": {
                    "name": "Integration Test",
                    "email": "test@compozerr.com"
                }
            },
            "pusher": {
                "name": "test",
                "email": "test@compozerr.com"
            },
            "sender": {
                "login": "test",
                "id": 1
            }
        }
        """;

        return JsonSerializer.Deserialize<PushEvent>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
