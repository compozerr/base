using Api.Abstractions;

namespace Cli.Abstractions;

/// <summary>
/// Defines template repositories that can be used to create multiple projects.
/// These repositories are reusable and do not enforce uniqueness per user.
/// </summary>
public static class TemplateRepositories
{
    private static readonly HashSet<(string, ProjectType)> TemplateRepoPatterns = new()
    {
        ("compozerr/n8n-template", ProjectType.N8n),
        ("compozerr/n8n-template.git", ProjectType.N8n),
    };

    /// <summary>
    /// Checks if a repository URL is a template repository.
    /// Template repositories can be used to create multiple projects by the same user.
    /// </summary>
    public static bool IsTemplateRepository(string repoUrl, out ProjectType? projectType)
    {
        try
        {
            var uri = new Uri(repoUrl);
            var repoPath = uri.AbsolutePath.TrimStart('/').TrimEnd('/');

            var match = TemplateRepoPatterns.FirstOrDefault(x => x.Item1.Equals(repoPath, StringComparison.OrdinalIgnoreCase));
            if (match != default)
            {
                projectType = match.Item2;
                return true;
            }

            projectType = null;
            return false;
        }
        catch
        {
            projectType = null;
            return false;
        }
    }
}
