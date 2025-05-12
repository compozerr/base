
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cli.Endpoints.Modules.Add;
public sealed record Commit(string Sha);

public sealed class CompozerrFile
{
    public Dictionary<string, string>? Dependencies { get; set; }
}

public sealed class Github(string Organization, string ModuleName, string? CommitHash) : IDisposable
{
    private readonly HttpClient SharedHttpClient = new()
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

    private record GitHubFileContent(string Content, string Encoding);
    private record RepositoryInfo(string DefaultBranch);
    private record BranchInfo(Commit Commit);
}