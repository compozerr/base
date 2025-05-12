namespace Cli.Endpoints.Modules.Add;

public sealed record ModuleDto(string Name, string Hash)
{
    public string Organization => Name.Split('/')[0];
    public string ModuleName => Name.Split('/')[1];
    public string FullName => $"{Organization}/{ModuleName}";
    public string CommitHash => Hash;
}

public class ModulesGetter
{
    private static async Task<List<ModuleDto>> GetModuleDependenciesAsync(
        string organization,
        string moduleName,
        string? commitHash,
        CancellationToken cancellationToken)
    {
        var github = new Github(organization, moduleName, commitHash);
        var (compozerrFile, foundHash) = await github.GetCompozerrFileAsync(cancellationToken);

        if (compozerrFile?.Dependencies is null || compozerrFile.Dependencies.Count == 0)
            return new List<ModuleDto>();

        return compozerrFile.Dependencies
            .Select(dependency => new ModuleDto(dependency.Key, dependency.Value))
            .ToList();
    }

    public static async Task<ModuleDto[]> GetModulesAsync(
        string organization,
        string moduleName,
        string? commitHash,
        int maxDepth = 5,
        int maxDependencies = 50,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ModuleDto>();
        var visited = new HashSet<string>(); // Track visited module names (not versions)
        var queue = new Queue<(string Org, string Module, string? Hash, int Depth, List<string> Path)>();

        // Start with the root module
        queue.Enqueue((organization, moduleName, commitHash, 0, new List<string>()));

        while (queue.Count > 0 && result.Count < maxDependencies)
        {
            var (currentOrg, currentModule, currentHash, currentDepth, currentPath) = queue.Dequeue();
            var moduleFullName = $"{currentOrg}/{currentModule}";

            // Check for circular dependency
            if (currentPath.Contains(moduleFullName))
            {
                var cycle = string.Join(" -> ", currentPath) + " -> " + moduleFullName;
                throw new InvalidOperationException($"Circular dependency detected: {cycle}");
            }

            // Skip if we've already visited this module (regardless of version)
            if (!visited.Add(moduleFullName))
                continue;

            try
            {
                // Get the actual commit hash if not provided
                var github = new Github(currentOrg, currentModule, currentHash);
                var (_, actualHash) = await github.GetCompozerrFileAsync(cancellationToken);

                // Add this module to the result
                result.Add(new ModuleDto(moduleFullName, actualHash));

                // Check if we've reached max depth
                if (currentDepth >= maxDepth)
                    continue;

                // Get dependencies and queue them
                var dependencies = await GetModuleDependenciesAsync(
                    currentOrg,
                    currentModule,
                    actualHash,
                    cancellationToken);

                foreach (var dependency in dependencies)
                {
                    var newPath = new List<string>(currentPath) { moduleFullName };
                    queue.Enqueue((
                        dependency.Organization,
                        dependency.ModuleName,
                        dependency.CommitHash,
                        currentDepth + 1,
                        newPath));
                }
            }
            catch (Exception ex)
            {
                // Log the error and continue with other modules
                Console.WriteLine($"Error processing module {moduleFullName}: {ex.Message}");
                // Optionally, you might want to remove this module from visited 
                // if you want to retry it later with a different version
            }
        }

        return result.ToArray();
    }
}