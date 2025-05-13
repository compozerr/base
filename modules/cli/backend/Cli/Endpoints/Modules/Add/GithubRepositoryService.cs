using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cli.Endpoints.Modules.Add;
public sealed record Commit(string Sha);

public sealed class CompozerrFile
{
    public Dictionary<string, string>? Dependencies { get; set; }
}

public sealed class GithubRepositoryService : IDisposable
{
    private readonly HttpClient SharedHttpClient;
    private readonly string Organization;
    private readonly string ModuleName;
    private readonly string? CommitHash;
    private readonly string? ClientId;
    private readonly string? ClientSecret;
    private readonly int RateLimit = 60; // Default unauthenticated rate limit

    public GithubRepositoryService(string organization, string moduleName, string? commitHash, string? clientId = null, string? clientSecret = null)
    {
        Organization = organization;
        ModuleName = moduleName;
        CommitHash = commitHash;
        ClientId = clientId;
        ClientSecret = clientSecret;

        SharedHttpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                UserAgent = { new ProductInfoHeaderValue("Compozerr", "1.0") },
                Accept =
                {
                    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"),
                    new MediaTypeWithQualityHeaderValue("application/json")
                }
            }
        };

        // Configure authentication if credentials are provided
        if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret))
        {
            var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{ClientId}:{ClientSecret}"));
            SharedHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            RateLimit = 5000; // Authenticated rate limit
        }
    }

    

    public async Task<(CompozerrFile?, string)> GetCompozerrFileAsync(CancellationToken cancellationToken = default)
    {
        var commitHash = await GetCommitHashAsync(CommitHash, cancellationToken);

        try
        {
            var rawUrl = $"https://raw.githubusercontent.com/{Organization}/{ModuleName}/{commitHash}/compozerr.json";
            var response = await SharedHttpClient.GetFromJsonAsync<CompozerrFile>(rawUrl, cancellationToken);
            return (response, commitHash);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // File doesn't exist - return empty dependencies
            return (new CompozerrFile { Dependencies = new Dictionary<string, string>() }, commitHash);
        }
    }

    private async Task<string> GetCommitHashAsync(string? commitHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(commitHash))
        {
            return await GetLatestHashAsync(cancellationToken);
        }

        if (commitHash.Length == 40)
        {
            return commitHash;
        }

        if (commitHash.Length < 40)
        {
            return await FindCommitHashAsync(commitHash, cancellationToken);
        }

        throw new ArgumentOutOfRangeException(
            nameof(commitHash),
            commitHash,
            "Commit hash must be 40 characters or less");
    }

    private async Task<string> FindCommitHashAsync(string partialHash, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{Organization}/{ModuleName}/commits";

        try
        {
            var response = await SharedHttpClient.GetFromJsonAsync<Commit[]>(url, cancellationToken);

            if (response == null || response.Length == 0)
            {
                throw new InvalidOperationException($"No commits found for {Organization}/{ModuleName}");
            }

            var commit = response.FirstOrDefault(c => c.Sha.StartsWith(partialHash, StringComparison.OrdinalIgnoreCase));

            if (commit == null)
            {
                return await GetLatestHashAsync(cancellationToken);
            }

            return commit.Sha;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to fetch commits for {Organization}/{ModuleName}: {ex.Message}", ex);
        }
    }

    private async Task<string> GetLatestHashAsync(CancellationToken cancellationToken = default)
    {
        // Try to get the default branch first
        try
        {
            var repoUrl = $"https://api.github.com/repos/{Organization}/{ModuleName}";
            var repoInfo = await SharedHttpClient.GetFromJsonAsync<RepositoryInfo>(repoUrl, cancellationToken);

            if (repoInfo?.DefaultBranch != null)
            {
                var branchUrl = $"https://api.github.com/repos/{Organization}/{ModuleName}/branches/{repoInfo.DefaultBranch}";
                var branchInfo = await SharedHttpClient.GetFromJsonAsync<BranchInfo>(branchUrl, cancellationToken);

                if (branchInfo?.Commit?.Sha != null)
                {
                    return branchInfo.Commit.Sha;
                }
            }
        }
        catch
        {
            // Fall back to commits endpoint
        }

        // Fallback to commits endpoint
        var url = $"https://api.github.com/repos/{Organization}/{ModuleName}/commits";
        var response = await SharedHttpClient.GetFromJsonAsync<Commit[]>(url, cancellationToken);

        if (response == null || response.Length == 0)
        {
            throw new InvalidOperationException($"No commits found for {Organization}/{ModuleName}");
        }

        return response[0].Sha;
    }

    public void Dispose()
    {
        SharedHttpClient.Dispose();
    }

    // Helper method to check the current rate limit
    public async Task<RateLimitInfo> GetRateLimitInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "https://api.github.com/rate_limit";
            var response = await SharedHttpClient.GetFromJsonAsync<RateLimitResponse>(url, cancellationToken);
            return response?.Resources?.Core ?? new RateLimitInfo(RateLimit, 0, 0);
        }
        catch
        {
            // Return default values if we can't get the rate limit info
            return new RateLimitInfo(RateLimit, 0, 0);
        }
    }

    private record GitHubFileContent(string Content, string Encoding);
    private record RepositoryInfo(string DefaultBranch);
    private record BranchInfo(Commit Commit);
    private record RateLimitResponse(RateLimitResources Resources);
    private record RateLimitResources(RateLimitInfo Core);
    public record RateLimitInfo(int Limit, int Remaining, long Reset);
}