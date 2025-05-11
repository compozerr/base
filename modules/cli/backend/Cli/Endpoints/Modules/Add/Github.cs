using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Cli.Endpoints.Modules.Add;

public sealed record Commit(string Sha);

public sealed class CompozerrFile
{
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; }
}

public sealed class Github(string Organization, string ModuleName, string? CommitHash)
{
    private HttpClient? _httpClient;

    private HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Compozerr", "1.0"));
            }
            return _httpClient;
        }
    }

    public async Task<(CompozerrFile?, string)> GetCompozerrFileAsync(CancellationToken cancellationToken = default)
    {
        var commitHash = await GetCommitHashAsync(CommitHash, cancellationToken);

        var url = $"https://raw.githubusercontent.com/{Organization}/{ModuleName}/{commitHash}/compozerr.json";
        var response = await HttpClient.GetFromJsonAsync<CompozerrFile>(url, cancellationToken);
        return (response, commitHash);
    }

    private async Task<string> GetCommitHashAsync(string? commitHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(commitHash))
        {
            return await GetLatestHashAsync(cancellationToken);
        }
        else if (commitHash.Length < 40)
        {
            return await FindCommitHashAsync(commitHash, cancellationToken);
        }
        else if (commitHash.Length > 40)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commitHash),
                commitHash,
                "Commit hash must be 40 characters long");
        }

        return commitHash;
    }

    private async Task<string> FindCommitHashAsync(string commitHash, CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{Organization}/{ModuleName}/commits";
        var response = await HttpClient.GetFromJsonAsync<Commit[]>(url, cancellationToken);
        if (response == null || response.Length == 0)
        {
            throw new Exception("No commits found");
        }

        var commit = response.FirstOrDefault(c => c.Sha.StartsWith(commitHash)) ?? throw new Exception($"Commit hash {commitHash} not found");
        return commit.Sha;
    }

    private async Task<string> GetLatestHashAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{Organization}/{ModuleName}/commits";
        var response = await HttpClient.GetFromJsonAsync<Commit[]>(url, cancellationToken);
        if (response == null || response.Length == 0)
        {
            throw new Exception("No commits found");
        }

        return response[0].Sha;
    }
}