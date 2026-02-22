using Octokit;
using Serilog;

namespace Github.Services;

public record ModuleChangeSet(
    string ModuleName,
    IReadOnlyList<ModuleFileChange> ChangedFiles);

public record ModuleFileChange(
    string FullPath,
    string RelativePath,
    string Status);

public interface IModuleSyncService
{
    /// <summary>
    /// Analyze a push event to determine which modules have changes.
    /// Returns list of (moduleName, changedFiles) tuples.
    /// </summary>
    Task<IReadOnlyList<ModuleChangeSet>> DetectModuleChangesAsync(
        IGitHubClient client,
        string repoOwner,
        string repoName,
        string beforeSha,
        string afterSha);

    /// <summary>
    /// Sync changes for a single module to its source repo.
    /// Creates a new commit on the module source repo's main branch.
    /// </summary>
    Task<string> SyncModuleToSourceRepoAsync(
        IGitHubClient projectClient,
        string projectOwner,
        string projectName,
        string afterSha,
        string moduleName,
        IReadOnlyList<ModuleFileChange> changedFiles,
        IGitHubClient moduleClient,
        string moduleOwner,
        string moduleRepoName,
        string commitMessage);
}

public sealed class ModuleSyncService : IModuleSyncService
{
    private const string ModulesPrefix = "modules/";

    public async Task<IReadOnlyList<ModuleChangeSet>> DetectModuleChangesAsync(
        IGitHubClient client,
        string repoOwner,
        string repoName,
        string beforeSha,
        string afterSha)
    {
        // Handle first push (beforeSha is all zeros)
        if (beforeSha == "0000000000000000000000000000000000000000")
        {
            var commit = await client.Repository.Commit.Get(repoOwner, repoName, afterSha);
            return GroupFilesByModule(commit.Files
                .Where(f => f.Filename.StartsWith(ModulesPrefix))
                .Select(f => new ModuleFileChange(f.Filename, RemoveModulePrefix(f.Filename), f.Status))
                .ToList());
        }

        var comparison = await client.Repository.Commit.Compare(repoOwner, repoName, beforeSha, afterSha);

        var moduleFiles = comparison.Files
            .Where(f => f.Filename.StartsWith(ModulesPrefix))
            .Select(f => new ModuleFileChange(f.Filename, RemoveModulePrefix(f.Filename), f.Status))
            .ToList();

        return GroupFilesByModule(moduleFiles);
    }

    public async Task<string> SyncModuleToSourceRepoAsync(
        IGitHubClient projectClient,
        string projectOwner,
        string projectName,
        string afterSha,
        string moduleName,
        IReadOnlyList<ModuleFileChange> changedFiles,
        IGitHubClient moduleClient,
        string moduleOwner,
        string moduleRepoName,
        string commitMessage)
    {
        // 1. Get current main branch ref on module source repo
        var mainRef = await moduleClient.Git.Reference.Get(moduleOwner, moduleRepoName, "heads/main");
        var currentSha = mainRef.Object.Sha;

        // 2. Build tree items for the new tree
        var treeItems = new List<NewTreeItem>();

        foreach (var file in changedFiles)
        {
            if (file.Status is "removed")
            {
                // For deleted files, add a tree item with null sha to remove it
                treeItems.Add(new NewTreeItem
                {
                    Path = file.RelativePath,
                    Mode = "100644",
                    Type = TreeType.Blob,
                    Sha = null
                });
                continue;
            }

            // Get file content from project repo at afterSha
            try
            {
                var rawContent = await projectClient.Repository.Content.GetRawContentByRef(
                    projectOwner, projectName, file.FullPath, afterSha);

                // Create blob on module repo
                var blob = await moduleClient.Git.Blob.Create(moduleOwner, moduleRepoName,
                    new NewBlob
                    {
                        Content = Convert.ToBase64String(rawContent),
                        Encoding = EncodingType.Base64
                    });

                treeItems.Add(new NewTreeItem
                {
                    Path = file.RelativePath,
                    Mode = "100644",
                    Type = TreeType.Blob,
                    Sha = blob.Sha
                });
            }
            catch (Exception ex)
            {
                Log.ForContext("File", file.FullPath)
                   .ForContext("Module", moduleName)
                   .Warning(ex, "Failed to sync file, skipping");
            }
        }

        if (treeItems.Count == 0)
        {
            Log.ForContext("Module", moduleName)
               .Information("No tree items to sync after processing, skipping commit");
            return currentSha;
        }

        // 3. Create new tree based on current tree
        var newTree = new NewTree { BaseTree = currentSha };
        foreach (var item in treeItems)
        {
            newTree.Tree.Add(item);
        }

        var createdTree = await moduleClient.Git.Tree.Create(moduleOwner, moduleRepoName, newTree);

        // 4. Create commit
        var newCommit = new NewCommit(
            $"sync: {commitMessage} (from {projectOwner}/{projectName})",
            createdTree.Sha,
            currentSha);

        var createdCommit = await moduleClient.Git.Commit.Create(moduleOwner, moduleRepoName, newCommit);

        // 5. Update main branch ref
        await moduleClient.Git.Reference.Update(moduleOwner, moduleRepoName, "heads/main",
            new ReferenceUpdate(createdCommit.Sha));

        Log.ForContext("Module", moduleName)
           .ForContext("CommitSha", createdCommit.Sha)
           .ForContext("FilesChanged", changedFiles.Count)
           .Information("Successfully synced module to source repo");

        return createdCommit.Sha;
    }

    private static string RemoveModulePrefix(string path)
    {
        // modules/{moduleName}/rest/of/path → rest/of/path
        var withoutModules = path[ModulesPrefix.Length..];
        var slashIndex = withoutModules.IndexOf('/');
        return slashIndex >= 0 ? withoutModules[(slashIndex + 1)..] : withoutModules;
    }

    private static string ExtractModuleName(string path)
    {
        // modules/{moduleName}/rest/of/path → moduleName
        var withoutModules = path[ModulesPrefix.Length..];
        var slashIndex = withoutModules.IndexOf('/');
        return slashIndex >= 0 ? withoutModules[..slashIndex] : withoutModules;
    }

    private static IReadOnlyList<ModuleChangeSet> GroupFilesByModule(IReadOnlyList<ModuleFileChange> files)
    {
        return files
            .GroupBy(f => ExtractModuleName(f.FullPath))
            .Select(g => new ModuleChangeSet(
                g.Key,
                g.ToList()))
            .ToList();
    }
}
