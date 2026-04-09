namespace Github.Services;

public sealed class InstallationNotFoundForRepoException : Exception
{
    public string RepoUri { get; }

    public InstallationNotFoundForRepoException(string repoUri, Exception? innerException = null)
        : base(BuildMessage(repoUri), innerException)
    {
        RepoUri = repoUri;
    }

    private static string BuildMessage(string repoUri) =>
        $"No GitHub App installation has access to '{repoUri}'. " +
        "Install the Compozerr GitHub App on the account that owns this repository, " +
        "or grant it access to the repository in your GitHub App settings.";
}
