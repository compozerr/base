using System.Text.RegularExpressions;

namespace Api.Data;

public static partial class SubdomainHelper
{
    /// <summary>
    /// Sanitizes an input string for use as part of a DNS subdomain label.
    /// Replaces non-alphanumeric characters with dashes, collapses consecutive dashes,
    /// and trims leading/trailing dashes.
    /// </summary>
    public static string SanitizeForSubdomain(string input)
    {
        var replaced = new string(input
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray());

        replaced = ConsecutiveDashesRegex().Replace(replaced, "-");

        return replaced.Trim('-');
    }

    /// <summary>
    /// Generates a subdomain from a branch name and project name (e.g., "feat-login-myproject").
    /// The result is truncated to 63 characters (max DNS label length).
    /// </summary>
    public static string GenerateSubdomain(string projectName, string branch)
    {
        var sanitizedProject = SanitizeForSubdomain(projectName);
        var sanitizedBranch = SanitizeForSubdomain(branch);

        var subdomain = $"{sanitizedBranch}-{sanitizedProject}";
        return TruncateSubdomain(subdomain);
    }

    /// <summary>
    /// Generates a PR-based subdomain (e.g., "pr-42-myproject").
    /// The result is truncated to 63 characters (max DNS label length).
    /// </summary>
    public static string GeneratePrSubdomain(string projectName, int prNumber)
    {
        var sanitizedProject = SanitizeForSubdomain(projectName);
        var subdomain = $"pr-{prNumber}-{sanitizedProject}";
        return TruncateSubdomain(subdomain);
    }

    private static string TruncateSubdomain(string subdomain)
        => subdomain.Length > 63 ? subdomain[..63].TrimEnd('-') : subdomain;

    [GeneratedRegex("-{2,}")]
    private static partial Regex ConsecutiveDashesRegex();
}
