using Github.Services;
using Moq;
using Octokit;
using Organizations.Data.Repositories;

namespace Github.Tests;

public class RepoInstallationLookupTests
{
    [Fact]
    public async Task GetInstallationClientForRepoAsync_ResolvesInstallation_ReturnsClient()
    {
        // Arrange — mock the app-level client's GetRepositoryInstallationForCurrent
        var mockAppClient = new Mock<IGitHubClient>();
        var mockGitHubAppsClient = new Mock<IGitHubAppsClient>();

        // GetRepositoryInstallationForCurrent returns an Installation object.
        // Octokit's Installation has no public ctor, so we use reflection.
        var installation = CreateInstallation(id: 58714671, login: "tomilodk");

        mockGitHubAppsClient
            .Setup(a => a.GetRepositoryInstallationForCurrent("tomilodk", "fusee"))
            .ReturnsAsync(installation);

        mockAppClient.Setup(c => c.GitHubApps).Returns(mockGitHubAppsClient.Object);

        var repoUri = new Uri("https://github.com/tomilodk/fusee.git");

        // Act
        var installationId = await RepoInstallationLookup.LookupInstallationIdAsync(
            mockAppClient.Object, repoUri);

        // Assert
        Assert.Equal(58714671, installationId);
    }

    [Fact]
    public async Task GetInstallationClientForRepoAsync_NoInstallation_ThrowsInstallationNotFound()
    {
        // Arrange
        var mockAppClient = new Mock<IGitHubClient>();
        var mockGitHubAppsClient = new Mock<IGitHubAppsClient>();

        mockGitHubAppsClient
            .Setup(a => a.GetRepositoryInstallationForCurrent("mappso", "mappso-blog"))
            .ThrowsAsync(new NotFoundException("Not Found", System.Net.HttpStatusCode.NotFound));

        mockAppClient.Setup(c => c.GitHubApps).Returns(mockGitHubAppsClient.Object);

        var repoUri = new Uri("https://github.com/mappso/mappso-blog.git");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InstallationNotFoundForRepoException>(
            () => RepoInstallationLookup.LookupInstallationIdAsync(mockAppClient.Object, repoUri));

        Assert.Contains("mappso/mappso-blog", ex.RepoUri);
    }

    // Helper — Octokit's Installation has no public constructor
    private static Installation CreateInstallation(long id, string login)
    {
        var type = typeof(Installation);
        var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

        // Id lives on the base class InstallationId, not on Installation itself
        SetBackingField(instance, type.BaseType!, "Id", id);

        // Installation.Account is of type Account (abstract) — use User (concrete subclass)
        var accountType = typeof(User);
        var account = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(accountType);
        SetBackingField(account, accountType, "Login", login);
        SetBackingField(instance, type, "Account", account);

        return (Installation)instance;
    }

    private static void SetBackingField(object instance, Type type, string propertyName, object value)
        => OctokitTestHelpers.SetBackingField(instance, type, propertyName, value);
}
