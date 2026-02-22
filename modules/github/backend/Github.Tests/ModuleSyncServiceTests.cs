using Github.Services;
using Moq;
using Octokit;

namespace Github.Tests;

public class ModuleSyncServiceTests
{
    private readonly ModuleSyncService _sut = new();

    [Fact]
    public async Task DetectModuleChanges_FiltersOnlyModuleFiles()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult(
            ("modules/stripe/backend/Stripe.cs", "modified"),
            ("modules/auth/frontend/Login.tsx", "added"),
            ("backend/Api/Program.cs", "modified"),
            ("frontend/src/App.tsx", "modified"),
            ("README.md", "modified"));

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ModuleName == "stripe");
        Assert.Contains(result, r => r.ModuleName == "auth");
    }

    [Fact]
    public async Task DetectModuleChanges_GroupsFilesByModule()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult(
            ("modules/stripe/backend/Stripe.cs", "modified"),
            ("modules/stripe/backend/StripeFeature.cs", "modified"),
            ("modules/stripe/frontend/index.tsx", "added"),
            ("modules/auth/backend/Auth.cs", "modified"));

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        var stripeChanges = result.Single(r => r.ModuleName == "stripe");
        Assert.Equal(3, stripeChanges.ChangedFiles.Count);

        var authChanges = result.Single(r => r.ModuleName == "auth");
        Assert.Equal(1, authChanges.ChangedFiles.Count);
    }

    [Fact]
    public async Task DetectModuleChanges_RemapsRelativePaths()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult(
            ("modules/stripe/backend/Stripe/Services/StripeService.cs", "modified"));

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        var file = result.Single().ChangedFiles.Single();
        Assert.Equal("modules/stripe/backend/Stripe/Services/StripeService.cs", file.FullPath);
        Assert.Equal("backend/Stripe/Services/StripeService.cs", file.RelativePath);
    }

    [Fact]
    public async Task DetectModuleChanges_EmptyDiff_ReturnsEmpty()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult();

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectModuleChanges_NoModuleFiles_ReturnsEmpty()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult(
            ("backend/Api/Program.cs", "modified"),
            ("frontend/src/App.tsx", "added"));

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DetectModuleChanges_PreservesFileStatus()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        var compareResult = CreateCompareResult(
            ("modules/stripe/backend/New.cs", "added"),
            ("modules/stripe/backend/Old.cs", "removed"),
            ("modules/stripe/backend/Changed.cs", "modified"));

        mockClient.Setup(c => c.Repository.Commit.Compare(
                "owner", "repo", "before-sha", "after-sha"))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", "before-sha", "after-sha");

        // Assert
        var files = result.Single().ChangedFiles;
        Assert.Contains(files, f => f.RelativePath == "backend/New.cs" && f.Status == "added");
        Assert.Contains(files, f => f.RelativePath == "backend/Old.cs" && f.Status == "removed");
        Assert.Contains(files, f => f.RelativePath == "backend/Changed.cs" && f.Status == "modified");
    }

    [Fact]
    public async Task DetectModuleChanges_FirstPush_UsesCommitGet()
    {
        // Arrange — first push has all-zeros beforeSha
        var mockClient = new Mock<IGitHubClient>();
        var allZerosSha = "0000000000000000000000000000000000000000";

        var commitFiles = new List<GitHubCommitFile>
        {
            CreateCommitFile("modules/stripe/backend/Stripe.cs", "added")
        };
        var commit = CreateGitHubCommit(commitFiles);

        mockClient.Setup(c => c.Repository.Commit.Get("owner", "repo", "after-sha"))
            .ReturnsAsync(commit);

        // Act
        var result = await _sut.DetectModuleChangesAsync(
            mockClient.Object, "owner", "repo", allZerosSha, "after-sha");

        // Assert
        Assert.Single(result);
        Assert.Equal("stripe", result[0].ModuleName);

        // Verify Compare was NOT called
        mockClient.Verify(c => c.Repository.Commit.Compare(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // Helper methods

    private static CompareResult CreateCompareResult(params (string Filename, string Status)[] files)
    {
        var commitFiles = files
            .Select(f => CreateCommitFile(f.Filename, f.Status))
            .ToList();

        // CompareResult doesn't have a public constructor, so we use reflection
        var type = typeof(CompareResult);
        var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

        var filesProperty = type.GetProperty("Files");
        if (filesProperty != null)
        {
            // Octokit uses IReadOnlyList<GitHubCommitFile> for the Files property
            // Set it via the backing field
            var backingField = type.GetFields(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("Files", StringComparison.OrdinalIgnoreCase)
                                  || f.Name.Contains("<Files>"));

            if (backingField != null)
            {
                backingField.SetValue(instance, (IReadOnlyList<GitHubCommitFile>)commitFiles);
            }
        }

        return (CompareResult)instance;
    }

    private static GitHubCommit CreateGitHubCommit(IReadOnlyList<GitHubCommitFile> files)
    {
        var type = typeof(GitHubCommit);
        var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

        var backingField = type.GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.Name.Contains("Files", StringComparison.OrdinalIgnoreCase)
                              || f.Name.Contains("<Files>"));

        if (backingField != null)
        {
            backingField.SetValue(instance, files);
        }

        return (GitHubCommit)instance;
    }

    private static GitHubCommitFile CreateCommitFile(string filename, string status)
    {
        var type = typeof(GitHubCommitFile);
        var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

        SetBackingField(instance, type, "Filename", filename);
        SetBackingField(instance, type, "Status", status);

        return (GitHubCommitFile)instance;
    }

    private static void SetBackingField(object instance, Type type, string propertyName, object value)
    {
        var field = type.GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(f => f.Name.Contains(propertyName, StringComparison.OrdinalIgnoreCase)
                              || f.Name.Contains($"<{propertyName}>"));

        field?.SetValue(instance, value);
    }
}
