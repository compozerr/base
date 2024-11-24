using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Core.Extensions;

public static class ConfigurationBuilderExtensions
{
    const string ApiProjectName = "Api";
    const int MaxSolutionSearchLevels = 7;

    public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder builder)
    {
        var apiProjectPath = FindApiDirectory();

        return builder.SetBasePath(apiProjectPath)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true);
    }

    private static string FindApiDirectory()
    {
        // First try to find solution directory from entry assembly location
        var entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location
            ?? throw new InvalidOperationException("Could not determine entry assembly location");

        var currentDir = new DirectoryInfo(Path.GetDirectoryName(entryAssemblyLocation)
            ?? throw new InvalidOperationException("Could not determine current directory"));

        // Search up to MaxSolutionSearchLevels levels up for solution file
        var solutionDir = FindSolutionDirectory(currentDir, MaxSolutionSearchLevels);

        if (solutionDir == null)
        {
            // Fallback: try from current directory
            currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            solutionDir = FindSolutionDirectory(currentDir, MaxSolutionSearchLevels);

            if (solutionDir == null)
                throw new InvalidOperationException("Could not find solution directory");
        }

        // Look for Api project directory recursively
        var apiDir = FindApiDirectory(solutionDir);

        if (apiDir == null)
            throw new InvalidOperationException("Could not find Api project directory");

        return apiDir.FullName;
    }

    private static DirectoryInfo? FindSolutionDirectory(DirectoryInfo startDir, int maxLevels)
    {
        var currentDir = startDir;
        var levelsSearched = 0;

        while (currentDir != null && levelsSearched < maxLevels)
        {
            // Check for any .sln file
            if (currentDir.GetFiles("*.sln").Any())
                return currentDir;

            // Also check for common solution indicators
            if (currentDir.GetDirectories(".git").Any() ||
                currentDir.GetDirectories(".github").Any() ||
                currentDir.GetFiles("global.json").Any() ||
                currentDir.GetFiles("nuget.config", SearchOption.TopDirectoryOnly).Any())
            {
                // Search one more level up if we find these indicators
                var parentWithSln = currentDir.Parent?.GetFiles("*.sln").Any() ?? false;
                if (parentWithSln)
                    return currentDir.Parent;
                return currentDir;
            }

            currentDir = currentDir.Parent;
            levelsSearched++;
        }

        return null;
    }

    private static DirectoryInfo? FindApiDirectory(DirectoryInfo solutionDir)
    {
        // First look for exact "Api" directory
        var apiDir = solutionDir.GetDirectories(ApiProjectName, SearchOption.AllDirectories)
                               .FirstOrDefault();

        if (apiDir != null)
            return apiDir;

        // Then look for directories containing "Api" that have a .csproj file
        var potentialApiDirs = solutionDir.GetDirectories($"*{ApiProjectName}*", SearchOption.AllDirectories)
                                        .Where(d => d.GetFiles("*.csproj").Any())
                                        .ToList();

        // If we have exactly one match, use it
        if (potentialApiDirs.Count == 1)
            return potentialApiDirs[0];

        // If we have multiple matches, try to find the most likely candidate
        if (potentialApiDirs.Count > 1)
        {
            // Prefer directories that have "appsettings.json"
            var withSettings = potentialApiDirs.FirstOrDefault(d =>
                d.GetFiles("appsettings.json").Any());

            if (withSettings != null)
                return withSettings;

            // Or take the shortest named one (likely to be just "Api" rather than "ApiTests" etc)
            return potentialApiDirs.OrderBy(d => d.Name.Length).First();
        }

        return null;
    }
}