namespace Cli.Endpoints.Modules.Add;

public sealed record ModuleDto(string Name, string Hash)
{
    public string Organization => Name.Split('/')[0];
    public string ModuleName => Name.Split('/')[1];
    public string CommitHash => Hash;
}

public class ModulesGetter
{
    private static async Task<ModuleDto[]> GetModuleDependenciesAsync(
        string organization,
        string moduleName,
        string? commitHash,
        CancellationToken cancellationToken)
    {
        var github = new Github(organization, moduleName, commitHash);
        var (compozerrFile, foundHash) = await github.GetCompozerrFileAsync(cancellationToken);
        var module = new ModuleDto($"{organization}/{moduleName}", foundHash);

        if (compozerrFile?.Dependencies is null)
            return [module];

        return [module, .. compozerrFile.Dependencies.Select(dependency => new ModuleDto(dependency.Key, dependency.Value))];
    }

    public static async Task<ModuleDto[]> GetModulesAsync(
        string organization,
        string moduleName,
        string? commitHash,
        int maxDepth = 2,
        int maxDependencies = 5,
        CancellationToken cancellationToken = default)
    {
        var dependencies = await GetModuleDependenciesAsync(
            organization,
            moduleName,
            commitHash,
            cancellationToken);

        var allDependencies = new HashSet<ModuleDto>(new ModuleDtoComparer());
        var visited = new HashSet<string>(); // Track visited modules to prevent circular references
        var queue = new Queue<(ModuleDto Module, int Depth)>();

        // Add initial modules to the queue
        foreach (var module in dependencies)
        {
            queue.Enqueue((module, 1));
        }

        while (queue.Count > 0 && allDependencies.Count < maxDependencies)
        {
            var (currentModule, currentDepth) = queue.Dequeue();

            // Use module path as unique identifier (org/name@hash)
            var moduleId = $"{currentModule.Organization}/{currentModule.ModuleName}@{currentModule.CommitHash}";

            // Skip if already visited
            if (!visited.Add(moduleId))
                continue;

            allDependencies.Add(currentModule);

            // Skip fetching dependencies if we've reached max depth
            if (currentDepth >= maxDepth)
                continue;

            var subModules = await GetModuleDependenciesAsync(
            currentModule.Organization,
            currentModule.ModuleName,
            currentModule.CommitHash,
            cancellationToken);

            // Queue the sub-dependencies for processing
            foreach (var subModule in subModules)
            {
                queue.Enqueue((subModule, currentDepth + 1));
            }
        }

        return [.. allDependencies];
    }
}

internal class ModuleDtoComparer : IEqualityComparer<ModuleDto>
{
    public bool Equals(ModuleDto? x, ModuleDto? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.Organization == y.Organization &&
               x.ModuleName == y.ModuleName &&
               x.CommitHash == y.CommitHash;
    }

    public int GetHashCode(ModuleDto obj)
    {
        if (obj is null)
            return 0;

        var hashCode = new HashCode();
        hashCode.Add(obj.Organization);
        hashCode.Add(obj.ModuleName);
        hashCode.Add(obj.CommitHash);
        return hashCode.ToHashCode();
    }
}