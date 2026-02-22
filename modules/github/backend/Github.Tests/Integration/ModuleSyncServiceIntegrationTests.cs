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

        // First commit: add a file
        var (_, firstCommitSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "public class TestService { }"
            });

        // Second commit: modify the file — chain directly off first commit
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "public class TestService { public void DoWork() { } }"
            },
            parentSha: firstCommitSha);

        // Act
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

        // First commit: add a file
        var (_, firstCommitSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                [uniqueFile] = "// will be deleted"
            });

        // Second commit: delete the file — chain directly off first commit
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            filesToAdd: new Dictionary<string, string>(),
            filesToDelete: [uniqueFile],
            parentSha: firstCommitSha);

        // Act
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, firstCommitSha, afterSha);

        // Assert
        Assert.Single(result);
        var file = result[0].ChangedFiles.Single();
        Assert.Equal("removed", file.Status);
    }

    [SkippableFact]
    public async Task DetectModuleChanges_MultipleModulesInSinglePush_GroupsCorrectly()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Create a commit touching files in two different modules
        var (beforeSha, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/module-a/backend/ServiceA.cs"] = "public class ServiceA { }",
                ["modules/module-b/frontend/ComponentB.tsx"] = "export const B = () => <div/>;"
            });

        // Act
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, beforeSha, afterSha);

        // Assert
        Assert.Equal(2, result.Count);
        var moduleNames = result.Select(r => r.ModuleName).OrderBy(n => n).ToList();
        Assert.Equal("module-a", moduleNames[0]);
        Assert.Equal("module-b", moduleNames[1]);
        Assert.Single(result.First(r => r.ModuleName == "module-a").ChangedFiles);
        Assert.Single(result.First(r => r.ModuleName == "module-b").ChangedFiles);
    }

    [SkippableFact]
    public async Task DetectModuleChanges_NonModuleFilesIgnored()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Create a commit with both module files and root-level files
        var (beforeSha, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/Real.cs"] = "public class Real { }",
                ["backend/SomeController.cs"] = "public class SomeController { }",
                ["docs/guide.md"] = "# Guide"
            });

        // Act
        var result = await sut.DetectModuleChangesAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, beforeSha, afterSha);

        // Assert: only the module file should appear
        Assert.Single(result);
        Assert.Equal("test-module", result[0].ModuleName);
        Assert.Single(result[0].ChangedFiles);
        Assert.Equal("backend/Real.cs", result[0].ChangedFiles[0].RelativePath);
    }

    [SkippableFact]
    public async Task SyncModuleToSourceRepo_MixedOperations_HandlesAddModifyDeleteTogether()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Setup: add a file to module repo that we'll delete via sync
        await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.ModRepoOwner,
            _fixture.ModRepoName,
            new Dictionary<string, string>
            {
                ["backend/Existing.cs"] = "public class Existing { }",
                ["backend/ToRemove.cs"] = "// will be removed"
            });

        // Create project repo state with the modified and new files (ToRemove is absent = deleted)
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/Existing.cs"] = "public class Existing { public void Updated() { } }",
                ["modules/test-module/backend/Brand-New.cs"] = "public class BrandNew { }"
            });

        var changedFiles = new List<ModuleFileChange>
        {
            new("modules/test-module/backend/Existing.cs", "backend/Existing.cs", "modified"),
            new("modules/test-module/backend/Brand-New.cs", "backend/Brand-New.cs", "added"),
            new("modules/test-module/backend/ToRemove.cs", "backend/ToRemove.cs", "removed")
        };

        // Act
        var commitSha = await sut.SyncModuleToSourceRepoAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, afterSha,
            "test-module", changedFiles,
            client, _fixture.ModRepoOwner, _fixture.ModRepoName,
            "feat: mixed operations");

        // Assert
        Assert.NotNull(commitSha);

        // Modified file should have new content
        var existingContent = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/Existing.cs", commitSha);
        Assert.NotNull(existingContent);
        Assert.Contains("Updated", existingContent);

        // New file should exist
        var newContent = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/Brand-New.cs", commitSha);
        Assert.NotNull(newContent);
        Assert.Contains("BrandNew", newContent);

        // Deleted file should be gone
        var removedContent = await GitHubTestHelper.ReadFileContentAsync(
            client, _fixture.ModRepoOwner, _fixture.ModRepoName, "backend/ToRemove.cs", commitSha);
        Assert.Null(removedContent);
    }

    [SkippableFact]
    public async Task SyncModuleToSourceRepo_CommitMessageHasSyncPrefix()
    {
        Skip.IfNot(_fixture.IsConfigured, "GitHub App credentials not configured");

        var client = _fixture.GitHubClient!;
        var sut = new ModuleSyncService();

        // Create a commit with a module file
        var (_, afterSha) = await GitHubTestHelper.CreateTestCommitAsync(
            client,
            _fixture.RepoOwner,
            _fixture.RepoName,
            new Dictionary<string, string>
            {
                ["modules/test-module/backend/Synced.cs"] = "public class Synced { }"
            });

        var changedFiles = new List<ModuleFileChange>
        {
            new("modules/test-module/backend/Synced.cs", "backend/Synced.cs", "added")
        };

        // Act
        var commitSha = await sut.SyncModuleToSourceRepoAsync(
            client, _fixture.RepoOwner, _fixture.RepoName, afterSha,
            "test-module", changedFiles,
            client, _fixture.ModRepoOwner, _fixture.ModRepoName,
            "feat: add synced file");

        // Assert: the commit message in the module repo must start with "sync:"
        // This is critical for loop prevention
        var commit = await client.Git.Commit.Get(
            _fixture.ModRepoOwner, _fixture.ModRepoName, commitSha);
        Assert.StartsWith("sync:", commit.Message);
        Assert.Contains(_fixture.RepoOwner, commit.Message);
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
