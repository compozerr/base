using Api.Abstractions;
using Api.Data.Repositories;
using Github.Abstractions;
using Github.Models;
using Github.Repositories;
using Github.Services;
using Github.Utils;
using Jobs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Github.Jobs;

public sealed class ModuleSyncProcessorJob(
    IPushWebhookEventRepository pushWebhookEventRepository,
    IModuleSyncEventRepository moduleSyncEventRepository,
    IGithubService githubService,
    IModuleSyncService moduleSyncService,
    IServiceScopeFactory serviceScopeFactory)
    : JobBase<ModuleSyncProcessorJob, PushWebhookEventId>
{
    public override async Task ExecuteAsync(PushWebhookEventId pushWebhookEventId)
    {
        var pushEvent = await pushWebhookEventRepository.GetByIdAsync(pushWebhookEventId);

        if (pushEvent is null)
        {
            Log.Warning("ModuleSyncProcessorJob: PushWebhookEvent {Id} not found", pushWebhookEventId);
            return;
        }

        await ProcessAsync(pushEvent);
    }

    private async Task ProcessAsync(PushWebhookEvent pushEvent)
    {
        try
        {
            // Extract repo URL
            if (Uri.TryCreate(pushEvent.Event.Repository?.CloneUrl, UriKind.Absolute, out var gitUrl) is false)
            {
                Log.Warning("ModuleSyncProcessorJob: Invalid repository clone URL for event {Id}", pushEvent.Id);
                return;
            }

            // Get project
            var projectId = await pushWebhookEventRepository.GetProjectIdFromGitUrlAsync(gitUrl);
            if (projectId is null)
            {
                Log.Information("ModuleSyncProcessorJob: No project found for URL {Url}, skipping", gitUrl);
                return;
            }

            // Get project details
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var project = await projectRepository.GetByIdAsync(projectId);

            if (project is null)
            {
                Log.Information("ModuleSyncProcessorJob: Project {ProjectId} not found, skipping", projectId);
                return;
            }

            // Check branch - only sync main
            var branch = pushEvent.Event.Ref.Replace("refs/heads/", string.Empty);
            if (branch is not "main")
            {
                Log.Information("ModuleSyncProcessorJob: Skipping non-main branch {Branch}", branch);
                return;
            }

            // Check for [skip sync]
            var commitMessage = pushEvent.Event.HeadCommit?.Message ?? "";
            if (commitMessage.Contains("[skip sync]"))
            {
                Log.Information("ModuleSyncProcessorJob: Skipping due to [skip sync] in commit message");
                return;
            }

            // Prevent sync loops - skip commits that were created by the sync process itself
            if (commitMessage.StartsWith("sync:"))
            {
                Log.Information("ModuleSyncProcessorJob: Skipping sync-generated commit to prevent loops");
                return;
            }

            // Resolve the installation that owns the project repo — we deliberately
            // do NOT trust any stored installation id, since it can go stale.
            GetInstallationClientForRepoResponse installationResponse;
            try
            {
                installationResponse = await githubService.GetInstallationClientForRepoAsync(project.RepoUri);
            }
            catch (InstallationNotFoundForRepoException ex)
            {
                Log.Information(ex,
                    "ModuleSyncProcessorJob: No GitHub App installation has access to {RepoUri}, skipping",
                    project.RepoUri);
                return;
            }

            var client = installationResponse.InstallationClient;
            var (repoOwner, repoName) = GitHubRepoUrl.Parse(gitUrl);

            // Detect module changes
            var beforeSha = pushEvent.Event.Before;
            var afterSha = pushEvent.Event.After;

            var changeSets = await moduleSyncService.DetectModuleChangesAsync(
                client, repoOwner, repoName, beforeSha, afterSha);

            if (changeSets.Count == 0)
            {
                Log.Information("ModuleSyncProcessorJob: No module changes detected in push event {Id}", pushEvent.Id);
                return;
            }

            // Get module repository from scoped service
            var moduleRepository = scope.ServiceProvider.GetRequiredService<IModuleRepository>();

            // Process each changed module
            foreach (var changeSet in changeSets)
            {
                await SyncModuleAsync(
                    pushEvent, projectId, changeSet,
                    client, repoOwner, repoName, afterSha, commitMessage,
                    moduleRepository);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ModuleSyncProcessorJob: Unhandled error processing push event {Id}", pushEvent.Id);
        }
    }

    private async Task SyncModuleAsync(
        PushWebhookEvent pushEvent,
        ProjectId projectId,
        ModuleChangeSet changeSet,
        Octokit.IGitHubClient projectClient,
        string repoOwner,
        string repoName,
        string afterSha,
        string commitMessage,
        IModuleRepository moduleRepository)
    {
        // Look up module in database
        var modules = await moduleRepository.GetFilteredAsync(m => m.Name == changeSet.ModuleName);
        var module = modules.FirstOrDefault();

        if (module is null)
        {
            // Module not registered - create skipped sync event
            await moduleSyncEventRepository.AddAsync(new ModuleSyncEvent
            {
                PushWebhookEventId = pushEvent.Id,
                ProjectId = projectId,
                ModuleName = changeSet.ModuleName,
                ModuleRepoUri = new Uri($"https://github.com/unknown/{changeSet.ModuleName}"),
                SourceCommitHash = afterSha,
                FilesChanged = changeSet.ChangedFiles.Count,
                Status = ModuleSyncStatus.Skipped,
                CompletedAt = DateTime.UtcNow
            });

            Log.Information("ModuleSyncProcessorJob: Module {Module} not registered, skipping sync", changeSet.ModuleName);
            return;
        }

        // Create sync event
        var syncEvent = await moduleSyncEventRepository.AddAsync(new ModuleSyncEvent
        {
            PushWebhookEventId = pushEvent.Id,
            ProjectId = projectId,
            ModuleName = changeSet.ModuleName,
            ModuleRepoUri = module.RepoUri,
            SourceCommitHash = afterSha,
            FilesChanged = changeSet.ChangedFiles.Count,
            Status = ModuleSyncStatus.Syncing
        });

        try
        {
            // Parse module source repo
            var (moduleOwner, moduleRepoName) = GitHubRepoUrl.Parse(module.RepoUri);

            // Resolve the installation that owns the module repo. The module
            // may live in a different GitHub account than the project, so we
            // can't reuse the project installation — look it up fresh from
            // the module's own repo URL.
            GetInstallationClientForRepoResponse moduleClientResponse;
            try
            {
                moduleClientResponse = await githubService.GetInstallationClientForRepoAsync(module.RepoUri);
            }
            catch (InstallationNotFoundForRepoException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve GitHub App installation for module repo. " +
                    $"Ensure the GitHub App is installed on the account that owns {module.RepoUri}",
                    ex);
            }

            var targetCommitSha = await moduleSyncService.SyncModuleToSourceRepoAsync(
                projectClient, repoOwner, repoName, afterSha,
                changeSet.ModuleName, changeSet.ChangedFiles,
                moduleClientResponse.InstallationClient, moduleOwner, moduleRepoName,
                commitMessage);

            syncEvent.TargetCommitHash = targetCommitSha;
            syncEvent.Status = ModuleSyncStatus.Succeeded;
            syncEvent.CompletedAt = DateTime.UtcNow;
            await moduleSyncEventRepository.UpdateAsync(syncEvent);

            Log.Information("ModuleSyncProcessorJob: Successfully synced module {Module} → {TargetSha}",
                changeSet.ModuleName, targetCommitSha);
        }
        catch (Exception ex)
        {
            syncEvent.Status = ModuleSyncStatus.Failed;
            syncEvent.ErrorMessage = ex.Message;
            syncEvent.CompletedAt = DateTime.UtcNow;
            await moduleSyncEventRepository.UpdateAsync(syncEvent);

            Log.Error(ex, "ModuleSyncProcessorJob: Failed to sync module {Module}", changeSet.ModuleName);
        }
    }
}
