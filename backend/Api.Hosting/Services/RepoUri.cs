namespace Api.Hosting.Services;

using System;
using System.Text.RegularExpressions;

public sealed record RepoUri(Uri Uri)
{
    private static readonly Regex RepoNameRegex = new(
        @"(?:https?:\/\/github\.com\/)([^\/]+\/[^\/\.]+)(?:\.git)?",
        RegexOptions.Compiled);

    public static RepoUri Parse(Uri uri)
    {
        return new RepoUri(uri);
    }

    public string RepoName
        => ExtractRepoName(Uri.ToString());

    private string ExtractRepoName(string url)
    {
        var match = RepoNameRegex.Match(url);
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"Could not extract repository name from URL: {url}");
    }
}