using System.Text.RegularExpressions;

namespace Github.Utils;

public static partial class GitHubRepoUrl
{
    // Matches SSH URLs like git@github.com:owner/repo.git
    [GeneratedRegex(@"^git@github\.com:([^/]+)/([^/]+?)(?:\.git)?$")]
    private static partial Regex SshUrlRegex();

    public static (string Owner, string RepoName) Parse(Uri repoUri)
    {
        var path = repoUri.AbsolutePath
            .TrimStart('/')
            .Replace(".git", "");

        var parts = path.Split('/');

        if (parts.Length < 2)
            throw new ArgumentException($"Invalid GitHub repository URL: {repoUri}", nameof(repoUri));

        return (parts[0], parts[1]);
    }

    public static (string Owner, string RepoName) Parse(string repoUrl)
    {
        // Try SSH format first
        var sshMatch = SshUrlRegex().Match(repoUrl);
        if (sshMatch.Success)
            return (sshMatch.Groups[1].Value, sshMatch.Groups[2].Value);

        return Parse(new Uri(repoUrl));
    }

    public static bool TryParse(Uri repoUri, out string owner, out string repoName)
    {
        owner = string.Empty;
        repoName = string.Empty;

        var path = repoUri.AbsolutePath
            .TrimStart('/')
            .Replace(".git", "");

        var parts = path.Split('/');

        if (parts.Length < 2)
            return false;

        owner = parts[0];
        repoName = parts[1];
        return true;
    }

    public static bool TryParse(string repoUrl, out string owner, out string repoName)
    {
        owner = string.Empty;
        repoName = string.Empty;

        // Try SSH format first: git@github.com:owner/repo.git
        var sshMatch = SshUrlRegex().Match(repoUrl);
        if (sshMatch.Success)
        {
            owner = sshMatch.Groups[1].Value;
            repoName = sshMatch.Groups[2].Value;
            return true;
        }

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return false;

        return TryParse(uri, out owner, out repoName);
    }
}
