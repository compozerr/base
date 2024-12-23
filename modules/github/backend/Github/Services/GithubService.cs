using Auth.Abstractions;
using Auth.Models;
using Auth.Repositories;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Repositories;
using Octokit;
using Serilog;

namespace Github.Services;

public record InstallationDto(string InstallationId, string Name, AccountType? AccountType);
public record RepositoryDto(string OwnedByInstallationId, string Name);

public interface IGithubService
{
    IGitHubClient GetClient();
    Task<IGitHubClient?> GetUserClient(UserId userId);
    IGitHubClient? GetUserClientByAccessToken(string userAccessToken);
    IGitHubClient? GetInstallationClientByAccessToken(string installationAccessToken);
    Task<IGitHubClient?> GetInstallationClientByInstallationIdAsync(string installationId);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserAsync(UserId userId);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserByAccessTokenAsync(string userAccessToken);
    Task<(IGitHubClient, string)> GetInstallationClientByUserDefaultAsync(UserId userId, DefaultInstallationIdSelectionType type);
    Task<IReadOnlyList<RepositoryDto>> GetRepositoriesByUserDefaultIdAsync(
        UserId userId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType);
}

public sealed class GithubService(
    IGithubJsonWebTokenService jwtService,
    IUserRepository userRepository,
    IGithubUserSettingsRepository githubUserSettingsRepository) : IGithubService
{
    private readonly Dictionary<string, (string, DateTimeOffset)> _installationTokensCache = [];

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

    private async Task<string> GetOrCreateInstallationTokenFromInstallationIdAsync(string installationId)
    {
        if (_installationTokensCache.TryGetValue(installationId, out var tokenInfo))
        {
            var (token, expiration) = tokenInfo;

            if (expiration > DateTimeOffset.Now)
                return token;
        }

        var client = GetClient();

        var installationToken = await client.GitHubApps.CreateInstallationToken(int.Parse(installationId));

        _installationTokensCache[installationId] = (installationToken.Token, installationToken.ExpiresAt);

        return installationToken.Token;
    }

    public async Task<IGitHubClient?> GetInstallationClientByInstallationIdAsync(string installationId)
    {
        var installationAccessToken = await GetOrCreateInstallationTokenFromInstallationIdAsync(installationId);

        return GetInstallationClientByAccessToken(installationAccessToken);
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

    public async Task<(IGitHubClient, string)> GetInstallationClientByUserDefaultAsync(UserId userId, DefaultInstallationIdSelectionType type)
    {
        var userSettings = await githubUserSettingsRepository.GetOrDefaultByUserIdAsync(userId);

        string? installationId = "";

        switch (type)
        {
            case DefaultInstallationIdSelectionType.Projects:
                installationId = userSettings!.SelectedProjectsInstallationId;
                break;
            case DefaultInstallationIdSelectionType.Modules:
                installationId = userSettings!.SelectedModulesInstallationId;
                break;
        }

        if (string.IsNullOrEmpty(installationId))
        {
            Log.ForContext(nameof(userId), userId)
               .ForContext(nameof(type), type)
               .Error("No installationId found for user");

            throw new ArgumentNullException(nameof(installationId), "No installationId found for user");
        }

        var installationClient = await GetInstallationClientByInstallationIdAsync(installationId);

        if (installationClient is null)
        {
            Log.ForContext(nameof(installationId), installationId)
               .Error("Failed to get installationClient");

            throw new ArgumentNullException(nameof(installationClient), "Failed to get installationClient with the given installationId");
        }

        return (installationClient, installationId);
    }

    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesByUserDefaultIdAsync(
        UserId userId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
    {
        var (installationClient, installationId) = await GetInstallationClientByUserDefaultAsync(userId, defaultInstallationIdSelectionType);

        try
        {
            var reposResponse = await installationClient.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            return reposResponse.Repositories.Select(r => new RepositoryDto(installationId, r.Name))
                                             .ToList();
        }
        catch (ForbiddenException exception)
        {
            Log.ForContext(nameof(installationId), installationId)
               .ForContext(nameof(exception), exception)
               .Warning("Installation does not have access to get repos");

            return [];
        }
    }
}