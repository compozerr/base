using Auth.Abstractions;
using Auth.Models;
using Github.Endpoints.SetDefaultInstallationId;
using Octokit;
using Serilog;

namespace Github.Services;

public sealed class MockGithubService : IGithubService
{
    public MockGithubService()
    {
        Log.Information("[Github] MockGithubService initialized (development mode)");
    }

    public IGitHubClient GetClient()
    {
        return new GitHubClient(new ProductHeaderValue("compozerr-dev"));
    }

    public Task<IGitHubClient?> GetUserClient(UserId userId)
    {
        return Task.FromResult<IGitHubClient?>(new GitHubClient(new ProductHeaderValue("compozerr-dev")));
    }

    public IGitHubClient? GetUserClientByAccessToken(string userAccessToken)
    {
        return new GitHubClient(new ProductHeaderValue("compozerr-dev"));
    }

    public IGitHubClient? GetInstallationClientByAccessToken(string installationAccessToken)
    {
        return new GitHubClient(new ProductHeaderValue("compozerr-dev"));
    }

    public Task<GetInstallationClientByInstallationIdResponse?> GetInstallationClientByInstallationIdAsync(string installationId)
    {
        var client = new GitHubClient(new ProductHeaderValue("compozerr-dev"));
        var response = new GetInstallationClientByInstallationIdResponse(client, "dev-token");
        return Task.FromResult<GetInstallationClientByInstallationIdResponse?>(response);
    }

    public Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserAsync(UserId userId)
    {
        IReadOnlyList<InstallationDto> installations = new List<InstallationDto>
        {
            new("dev-install-1", "dev-user", AccountType.User)
        };
        return Task.FromResult(installations);
    }

    public Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserByAccessTokenAsync(string userAccessToken)
    {
        IReadOnlyList<InstallationDto> installations = new List<InstallationDto>
        {
            new("dev-install-1", "dev-user", AccountType.User)
        };
        return Task.FromResult(installations);
    }

    public Task<GetInstallationClientByUserDefaultResponse> GetInstallationClientByUserDefaultAsync(
        UserId userId, DefaultInstallationIdSelectionType type)
    {
        var client = new GitHubClient(new ProductHeaderValue("compozerr-dev"));
        var response = new GetInstallationClientByUserDefaultResponse(client, "dev-install-1", "dev-token");
        return Task.FromResult(response);
    }

    public Task<IReadOnlyList<RepositoryDto>> GetRepositoriesByUserDefaultIdAsync(
        UserId userId, DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
    {
        IReadOnlyList<RepositoryDto> repos = new List<RepositoryDto>();
        return Task.FromResult(repos);
    }

    public Task<GithubUserLogin?> GetUserLoginAsync(UserId userId)
    {
        return Task.FromResult<GithubUserLogin?>(new GithubUserLogin
        {
            UserId = userId,
            Provider = Provider.GitHub,
            ProviderUserId = "dev-user-0",
            AccessToken = "dev-access-token"
        });
    }

    public Task<(Repository, Task)> ForkRepositoryAsync(
        IGitHubClient client, string owner, string repo, string organization, string name)
    {
        throw new NotSupportedException("ForkRepositoryAsync is not available in dev mock");
    }

    public Task<Reference> CreateBranchAsync(
        IGitHubClient client, string owner, string repo, string branchName, string baseBranchName = "main")
    {
        throw new NotSupportedException("CreateBranchAsync is not available in dev mock");
    }

    public Task<GitHubCommit> GetLatestCommitAsync(Uri repoUri, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GitHubCommit>(null!);
    }

    public Task<GitHubCommit> GetLatestCommitAsync(Uri repoUri, UserId? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GitHubCommit>(null!);
    }

    public Task<bool> HasAccessToRepositoryAsync(string repoUrl, UserId userId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ValidateTokenAsync(string accessToken)
    {
        return Task.FromResult(true);
    }
}
