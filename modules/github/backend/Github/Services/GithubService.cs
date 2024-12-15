using Auth.Abstractions;
using Auth.Repositories;
using Octokit;

namespace Github.Services;

public interface IGithubService
{
    IGitHubClient GetClient();
    Task<IGitHubClient?> GetUserClient(UserId userId);
}

public sealed class GithubService(
    IGithubJsonWebTokenService jwtService,
    IUserRepository userRepository) : IGithubService
{
    public IGitHubClient GetClient()
    {
        var token = jwtService.CreateToken();
        var client = GetGithubClientWithCredentials(new Credentials(token, AuthenticationType.Bearer));

        return client;
    }

    public async Task<IGitHubClient?> GetUserClient(UserId userId)
    {
        var user = await userRepository.GetUserWithLoginsAsync(userId);

        if (user is null)
            return null;

        var accessToken = (user.Logins.FirstOrDefault(l => l.Provider == Auth.Models.Provider.GitHub) as Auth.Models.GithubUserLogin)!.AccessToken;

        if (accessToken is null)
            return null;

        var userClient = GetGithubClientWithCredentials(new Credentials(accessToken, AuthenticationType.Oauth));

        return userClient;
    }

    private static GitHubClient GetGithubClientWithCredentials(Credentials credentials)
    {
        var client = new GitHubClient(new ProductHeaderValue("compozerr"))
        {
            Credentials = credentials
        };

        return client;
    }
}