using Auth.Abstractions;
using Github.Repositories;
using Octokit;

namespace Github.Services;

public interface IGithubService
{
    IGitHubClient GetClient();
    Task<IGitHubClient?> GetUserClient(UserId userId);
}

public sealed class GithubService : IGithubService
{
    private readonly IInstallationRepository _installationRepository;
    private readonly IGitHubClient _client;
    public GithubService(IGithubJsonWebTokenService jwtService, IInstallationRepository installationRepository)
    {
        var token = jwtService.CreateToken();
        _client = new GitHubClient(new ProductHeaderValue("compozerr"))
        {
            Credentials = new Credentials(token, AuthenticationType.Bearer)
        };
        _installationRepository = installationRepository;
    }

    public IGitHubClient GetClient()
    {
        return _client;
    }

    public async Task<IGitHubClient?> GetUserClient(UserId userId)
    {
        var installations = await _installationRepository.GetInstallationsByUserIdAsync(userId);

        var accessToken = installations.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault()?.AccessToken;

        if (accessToken is null)
        {
            return null;
        }

        var userClient = new GitHubClient(new ProductHeaderValue("compozerr"))
        {
            Credentials = new Credentials(accessToken, AuthenticationType.Oauth)
        };

        return userClient;
    }
}