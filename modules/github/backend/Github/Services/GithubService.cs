using Auth.Abstractions;
using Auth.Models;
using Auth.Repositories;
using Octokit;
using Serilog;

namespace Github.Services;

public record InstallationDto(string InstallationId, string Name, AccountType? AccountType);

public interface IGithubService
{
    IGitHubClient GetClient();
    Task<IGitHubClient?> GetUserClient(UserId userId);
    IGitHubClient? GetUserClientByAccessToken(string userAccessToken);
    IGitHubClient? GetInstallationClientByAccessToken(string installationAccessToken);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserAsync(UserId userId);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserByAccessTokenAsync(string userAccessToken);
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
        var userLogin = await GetUserLoginAsync(userId);

        if (userLogin?.AccessToken is null)
            return null;

        var userClient = GetGithubClientWithCredentials(new Credentials(userLogin.AccessToken, AuthenticationType.Oauth));

        return userClient;
    }

    public IGitHubClient? GetUserClientByAccessToken(string userAccessToken)
    {
        var userClient = GetGithubClientWithCredentials(new Credentials(userAccessToken, AuthenticationType.Oauth));

        return userClient;
    }

    public IGitHubClient? GetInstallationClientByAccessToken(string installationAccessToken)
    {
        var installationClient = GetGithubClientWithCredentials(new Credentials(installationAccessToken, AuthenticationType.Bearer));

        return installationClient;
    }

    private static GitHubClient GetGithubClientWithCredentials(Credentials credentials)
    {
        var client = new GitHubClient(new ProductHeaderValue("compozerr"))
        {
            Credentials = credentials
        };

        return client;
    }

    public async Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserAsync(UserId userId)
    {
        var userClient = await GetUserClient(userId);
        if (userClient is null)
        {
            Log.ForContext(nameof(userId), userId, true)
               .Error("Failed to get userClient");

            throw new ArgumentNullException(nameof(userId), "Failed to get userClient with the given userId");
        }

        return await GetInstallationsForUserUsingUserClientAsync(userClient);
    }

    public async Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserByAccessTokenAsync(string userAccessToken)
    {
        var userClient = GetUserClientByAccessToken(userAccessToken);
        if (userClient is null)
        {
            Log.Error("Failed to get userClient with given userAccessToken");

            throw new ArgumentNullException(nameof(userAccessToken), "Failed to get userClient with the given userAccessToken");
        }

        return await GetInstallationsForUserUsingUserClientAsync(userClient);
    }

    private async Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserUsingUserClientAsync(IGitHubClient userClient)
    {
        var userInstallations = await userClient.GitHubApps.GetAllInstallationsForCurrentUser();

        return userInstallations.Installations.Select(userInstallation => new InstallationDto(
            userInstallation.Id.ToString(),
            userInstallation.Account.Login,
            userInstallation.Account.Type)).ToList();
    }

    private async Task<GithubUserLogin?> GetUserLoginAsync(UserId userId)
    {
        var user = await userRepository.GetUserWithLoginsAsync(userId);

        if (user is null)
            return null;

        return (user.Logins.FirstOrDefault(l => l.Provider == Provider.GitHub) as GithubUserLogin)!;
    }
}