using Github.Utils;
using Octokit;

namespace Github.Services;

public static class RepoInstallationLookup
{
    public static async Task<long> LookupInstallationIdAsync(
        IGitHubClient appJwtClient,
        Uri repoUri)
    {
        var (owner, repoName) = GitHubRepoUrl.Parse(repoUri);

        try
        {
            var installation = await appJwtClient.GitHubApps
                .GetRepositoryInstallationForCurrent(owner, repoName);
            return installation.Id;
        }
        catch (NotFoundException ex)
        {
            throw new InstallationNotFoundForRepoException(repoUri.ToString(), ex);
        }
    }
}
