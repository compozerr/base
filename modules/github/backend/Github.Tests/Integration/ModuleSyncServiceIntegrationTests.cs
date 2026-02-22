using Github.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Github.Tests.Integration;

[Collection("GitHubIntegration")]
[Trait("Category", "Integration")]
public class ModuleSyncServiceIntegrationTests : IAsyncLifetime
{
    private readonly GitHubIntegrationTestFixture _fixture;

    public ModuleSyncServiceIntegrationTests(GitHubIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await _fixture.CleanupDatabaseAsync();

    public async Task DisposeAsync()
    {
        await _fixture.ResetReposAsync();
    }

    [SkippableFact]
    public async Task DetectModuleChanges_WithRealCommit_DetectsNewFiles()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Create a commit with a new module file
        var (beforeSha, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/TestService.cs"] = "public class TestService { }"
            });

        // Act
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, beforeSha, afterSha);

        // Assert
        Assert.Single(result);
        Assert.Equal("test-module", result[0].ModuleName);
        Assert.Single(result[0].ChangedFiles);
        Assert.Equal("backend/TestService.cs", result[0].ChangedFiles[0].RelativePath);
        Assert.Equal("added", result[0].ChangedFiles[0].Status);
    }

    [SkippableFact]
    public async Task DetectModuleChanges_WithModifiedFile_DetectsModification()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Use a unique file name to avoid interference from other tests
        var uniqueFile = $"modules/test-module/backend/ModifyTest_{Guid.NewGuid():N}.cs";

        // First commit: add a file — capture its afterSha to use as the compare base
        var (_, firstCommitSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "public class TestService { }"
            });

        // Second commit: modify the file
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "public class TestService { public void DoWork() { } }"
            });

        // Act — use firstCommitSha (not the second call's beforeSha) to guarantee
        // we're comparing the exact commits with proper parent chain
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, firstCommitSha, afterSha);

        // Assert
        Assert.Single(result);
        Assert.Equal("test-module", result[0].ModuleName);
        var file = result[0].ChangedFiles.Single();
        Assert.Equal("modified", file.Status);
    }

    [SkippableFact]
    public async Task DetectModuleChanges_WithDeletedFile_DetectsDeletion()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Use a unique file name to avoid interference from other tests
        var uniqueFile = $"modules/test-module/backend/DeleteTest_{Guid.NewGuid():N}.cs";

        // First commit: add a file — capture its afterSha to use as the compare base
        var (_, firstCommitSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "// will be deleted"
            });

        // Second commit: delete the file
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            filesToAdd: new Dictionary<string, string>(),
            filesToDelete: [uniqueFile]);

        // Act — use firstCommitSha to guarantee proper parent chain for compare
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, firstCommitSha, afterSha);

        // Assert
        Assert.Single(result);
        var file = result[0].ChangedFiles.Single();
        Assert.Equal("removed", file.Status);
    }

    [SkippableFact]
    public async Task SyncModuleToSourceRepo_AddsFilesToModuleRepo()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Create a commit with module files on the project repo
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/TestService.cs"] = "public class TestService { }",
                ["modules/test-module/backend/TestHelper.cs"] = "public class TestHelper { }"
            });

        var changedFiles = new List<ModuleFileChange>
        {
            new("modules/test-module/backend/TestService.cs", "backend/TestService.cs", "added"),
            new("modules/test-module/backend/TestHelper.cs", "backend/TestHelper.cs", "added")
        };

        // Act
        var commitSha = await sut.SyncModuleToSourceRepoAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, afterSha,
            "test-module", changedFiles,
            client, _fixture.ModRepoOwner, _fixture.ModRepoName,
            "test: add files");

        // Assert
        Assert.NotNull(commitSha);
        Assert.NotEqual(_fixture.ModuleBaselineSha, commitSha);

        // Verify files exist in module repo
        var content1 = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/TestService.cs", commitSha);
        Assert.NotNull(content1);
        Assert.Contains("TestService", content1);

        var content2 = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/TestHelper.cs", commitSha);
        Assert.NotNull(content2);
        Assert.Contains("TestHelper", content2);
    }

    [SkippableFact]
    public async Task SyncModuleToSourceRepo_DeletesFilesFromModuleRepo()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // First: add a file to the module repo directly
        await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.ModRepoOwner,
            _fixture.ModRepoName,
            new Dictionary<string, string>
            {
                ["backend/ToDelete.cs"] = "// this will be deleted via sync"
            });

        // Also create the file on the project repo, then delete it
        await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/ToDelete.cs"] = "// this will be deleted via sync"
            });

        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            filesToAdd: new Dictionary<string, string>(),
            filesToDelete: ["modules/test-module/backend/ToDelete.cs"]);

        var changedFiles = new List<ModuleFileChange>
        {
            new("modules/test-module/backend/ToDelete.cs", "backend/ToDelete.cs", "removed")
        };

        // Act
        var commitSha = await sut.SyncModuleToSourceRepoAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, afterSha,
            "test-module", changedFiles,
            client, _fixture.ModRepoOwner, _fixture.ModRepoName,
            "test: delete file");

        // Assert
        Assert.NotNull(commitSha);

        var content = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/ToDelete.cs", commitSha);
        Assert.Null(content);
    }
}
