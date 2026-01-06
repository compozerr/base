namespace Github.Utils;

public static class GitHubRepoUrl
{
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
        => Parse(new Uri(repoUrl));

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

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
            return false;

        return TryParse(uri, out owner, out repoName);
    }
}
