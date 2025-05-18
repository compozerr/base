using Auth.Abstractions;
using Auth.Models;
using Auth.Repositories;
using Core.Extensions;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Repositories;
using Octokit;
using Serilog;

namespace Github.Services;

public record InstallationDto(string InstallationId, string Name, AccountType? AccountType);
public record RepositoryDto(string OwnedByInstallationId, string Name);

public record GetInstallationClientByUserDefaultResponse(
    IGitHubClient InstallationClient,
    string InstallationId,
    string InstallationToken);

public record GetInstallationClientByInstallationIdResponse(
    IGitHubClient InstallationClient,
    string InstallationToken);

public interface IGithubService
{
    IGitHubClient GetClient();
    Task<IGitHubClient?> GetUserClient(UserId userId);
    IGitHubClient? GetUserClientByAccessToken(string userAccessToken);
    IGitHubClient? GetInstallationClientByAccessToken(string installationAccessToken);
    Task<GetInstallationClientByInstallationIdResponse?> GetInstallationClientByInstallationIdAsync(string installationId);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserAsync(UserId userId);
    Task<IReadOnlyList<InstallationDto>> GetInstallationsForUserByAccessTokenAsync(string userAccessToken);
    Task<GetInstallationClientByUserDefaultResponse> GetInstallationClientByUserDefaultAsync(UserId userId, DefaultInstallationIdSelectionType type);
    Task<IReadOnlyList<RepositoryDto>> GetRepositoriesByUserDefaultIdAsync(
        UserId userId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType);
    Task<GithubUserLogin?> GetUserLoginAsync(UserId userId);
    Task<(Repository, Task)> ForkRepositoryAsync(
        IGitHubClient client,
        string owner,
        string repo,
        string organization,
        string name);

    Task<Reference> CreateBranchAsync(
        IGitHubClient client,
        string owner,
        string repo,
        string branchName,
        string baseBranchName = "main");
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

    public async Task<GetInstallationClientByInstallationIdResponse?> GetInstallationClientByInstallationIdAsync(string installationId)
    {
        var installationAccessToken = await GetOrCreateInstallationTokenFromInstallationIdAsync(installationId);
        var client = GetInstallationClientByAccessToken(installationAccessToken);

        return client is not null
            ? new(client, installationAccessToken)
            : null;
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

    public async Task<GithubUserLogin?> GetUserLoginAsync(UserId userId)
    {
        try
        {
            var user = await userRepository.GetUserWithLoginsAsync(userId);

            if (user is null)
                return null;
            
            // Defensively check for null Logins collection
            if (user.Logins is null)
            {
                Log.ForContext(nameof(userId), userId)
                   .Warning("User found but Logins collection is null");
                return null;
            }

            // ToList() materializes the enumeration to avoid potential issues with deferred execution
            var logins = user.Logins.ToList();
            return (logins.FirstOrDefault(l => l.Provider == Provider.GitHub) as GithubUserLogin);
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(userId), userId)
               .ForContext("ExceptionType", ex.GetType().Name)
               .ForContext("ExceptionMessage", ex.Message)
               .Error(ex, "Exception occurred when retrieving GitHub user login");
            
            // Since this is called in various places, returning null is safer than throwing
            return null;
        }
    }

    public async Task<GetInstallationClientByUserDefaultResponse> GetInstallationClientByUserDefaultAsync(UserId userId, DefaultInstallationIdSelectionType type)
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

        var installationClientResponse = await GetInstallationClientByInstallationIdAsync(installationId);

        if (installationClientResponse is null)
        {
            Log.ForContext(nameof(installationId), installationId)
               .Error("Failed to get installationClient");

            throw new ArgumentNullException(nameof(installationClientResponse), "Failed to get installationClient with the given installationId");
        }

        return new(
            installationClientResponse.InstallationClient,
            installationId,
            installationClientResponse.InstallationToken);
    }

    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesByUserDefaultIdAsync(
        UserId userId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
    {
        var installationClientResponse = await GetInstallationClientByUserDefaultAsync(userId, defaultInstallationIdSelectionType);

        try
        {
            var reposResponse = await installationClientResponse.InstallationClient.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            return reposResponse.Repositories.Select(r => new RepositoryDto(installationClientResponse.InstallationId, r.Name))
                                             .ToList();
        }
        catch (ForbiddenException exception)
        {
            Log.ForContext(nameof(installationClientResponse.InstallationId), installationClientResponse.InstallationId)
               .ForContext(nameof(exception), exception)
               .Warning("Installation does not have access to get repos");

            return [];
        }
    }

    public async Task<(Repository, Task)> ForkRepositoryAsync(IGitHubClient client, string owner, string repo, string organization, string name)
    {
        var forkedRepo = await client.Repository.Forks.Create(
                            owner,
                            repo,
                            new NewRepositoryFork()
                            {
                                Organization = organization
                            }
                        );

        var repoResponse = await ConflictingRepoRetryOperation(
            async () =>
            {
                var repo = await client.Repository.Edit(
                organization,
                forkedRepo.Name,
                new RepositoryUpdate()
                {
                    Name = name,
                    Description = "Created by compozerr.com",
                });

                return repo;
            });

        return (repoResponse, WaitUntilExistsAsync(client, organization, forkedRepo.Name));
    }

    public static async Task<T> ConflictingRepoRetryOperation<T>(Func<Task<T>> operation, int maxRetries = 3, int initialDelayMs = 1000)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (ApiException ex) when (ex.HttpResponse.Body.ToString()?.Contains("A conflicting repository operation is still in progress") ?? false && retryCount < maxRetries)
            {
                retryCount++;
                var delayMs = initialDelayMs * (1 << (retryCount - 1)); // Exponential backoff
                await Task.Delay(delayMs);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }



    public async Task<Reference> CreateBranchAsync(
        IGitHubClient client,
        string owner,
        string repo,
        string branchName,
        string baseBranchName = "main")
    {
        return await ConflictingRepoRetryOperation(async () =>
            {
                var defaultBranch = await client.Git.Reference.Get(
                    owner,
                    repo,
                    $"heads/{baseBranchName}");

                var newBranch = await client.Git.Reference.Create(
                    owner,
                    repo,
                    new NewReference($"refs/heads/{branchName}", defaultBranch.Object.Sha)
                ).RetryAsync();
                return newBranch;
            });
    }

    private static async Task WaitUntilExistsAsync(IGitHubClient client, string owner, string repoName, int maxRetries = 10, int delayMs = 500)
    {
        int retryCount = 0;
        while (retryCount < maxRetries)
        {
            try
            {
                await client.Repository.Get(owner, repoName);
                return;
            }
            catch (ApiException)
            {
                retryCount++;
                await Task.Delay(delayMs * (1 << (retryCount - 1)));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error checking repository availability: {ex.Message}", ex);
            }
        }

        throw new TimeoutException($"Repository {owner}/{repoName} did not become available after forking");
    }
}