using Auth.Repositories;
using Github.Repositories;
using Github.Services;
using Moq;
using Octokit;

namespace Github.Tests;

public class GithubServiceTests
{
    private static GithubService CreateSut() =>
        new(
            Mock.Of<IGithubJsonWebTokenService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IGithubUserSettingsRepository>());

    [Fact]
    public async Task InstallationCanAccessRepoAsync_WhenRepositoryGetSucceeds_ReturnsTrue()
    {
        // Arrange
        var mockClient = new Mock<IGitHubClient>();
        mockClient
            .Setup(c => c.Repository.Get("tomilodk", "prebuild-tickets"))
            .ReturnsAsync((Repository)null!);

        var sut = CreateSut();

        // Act
        var result = await sut.InstallationCanAccessRepoAsync(
            mockClient.Object,
            new Uri("https://github.com/tomilodk/prebuild-tickets.git"));

        // Assert
        Assert.True(result);
        mockClient.Verify(c => c.Repository.Get("tomilodk", "prebuild-tickets"), Times.Once);
    }

    [Fact]
    public async Task InstallationCanAccessRepoAsync_WhenRepositoryGetThrowsNotFound_ReturnsFalse()
    {
        // Arrange — this is the real-world scenario from prod: stored installation
        // id belongs to an org that has no access to the user's repo, so GitHub
        // returns 404 for the repo lookup.
        var mockClient = new Mock<IGitHubClient>();
        mockClient
            .Setup(c => c.Repository.Get("tomilodk", "prebuild-tickets"))
            .ThrowsAsync(new NotFoundException(
                "Not Found",
                System.Net.HttpStatusCode.NotFound));

        var sut = CreateSut();

        // Act
        var result = await sut.InstallationCanAccessRepoAsync(
            mockClient.Object,
            new Uri("https://github.com/tomilodk/prebuild-tickets.git"));

        // Assert
        Assert.False(result);
    }
}
