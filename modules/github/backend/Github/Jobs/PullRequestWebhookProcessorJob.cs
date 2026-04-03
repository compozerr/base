using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Github.Abstractions;
using Github.Models;
using Github.Repositories;
using Jobs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Github.Jobs;

public sealed class PullRequestWebhookProcessorJob(
    IPullRequestWebhookEventRepository pullRequestWebhookEventRepository,
    IPushWebhookEventRepository pushWebhookEventRepository,
    IServiceScopeFactory serviceScopeFactory) : JobBase<PullRequestWebhookProcessorJob, PullRequestWebhookEventId>
{
    public override async Task ExecuteAsync(PullRequestWebhookEventId eventId)
    {
        var @event = await pullRequestWebhookEventRepository.GetByIdAsync(eventId);

        if (@event is null or { HandledAt: not null } or { ErroredAt: not null })
            return;

        await HandleEventAsync(@event);
    }

    private async Task HandleEventAsync(PullRequestWebhookEvent webhookEvent)
    {
        try
        {
            var prEvent = webhookEvent.Event;
            var action = prEvent.Action;

            Log.Information(
                "Processing PullRequestWebhookEvent {EventId} with action {Action}",
                webhookEvent.Id, action);

            if (prEvent.Repository?.CloneUrl is null ||
                !Uri.TryCreate(prEvent.Repository.CloneUrl, UriKind.Absolute, out var gitUrl))
            {
                throw new InvalidOperationException("Invalid repository clone URL in PR event.");
            }

            var projectId = await pushWebhookEventRepository.GetProjectIdFromGitUrlAsync(gitUrl);
            if (projectId is null)
            {
                Log.Information(
                    "No project found for git URL {GitUrl} in PullRequestWebhookEvent {EventId}",
                    gitUrl, webhookEvent.Id);
                await MarkAsHandledAsync(webhookEvent);
                return;
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var environmentRepository = scope.ServiceProvider.GetRequiredService<IProjectEnvironmentRepository>();
            var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            switch (action)
            {
                case "opened":
                case "reopened":
                case "synchronize":
                    await HandlePrOpenedOrSynchronizeAsync(
                        prEvent, projectId, environmentRepository, projectRepository, mediator);
                    break;

                case "closed":
                    await HandlePrClosedAsync(
                        prEvent, projectId, environmentRepository, mediator);
                    break;

                default:
                    Log.Information(
                        "Ignoring PR action {Action} for PullRequestWebhookEvent {EventId}",
                        action, webhookEvent.Id);
                    break;
            }

            await MarkAsHandledAsync(webhookEvent);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Error processing PullRequestWebhookEvent {EventId}",
                webhookEvent.Id);

            await MarkAsErroredAsync(webhookEvent, ex.Message);
        }
    }

    private async Task MarkAsHandledAsync(PullRequestWebhookEvent webhookEvent)
    {
        webhookEvent.HandledAt = DateTime.UtcNow;
        await pullRequestWebhookEventRepository.UpdateAsync(webhookEvent);
    }

    private async Task MarkAsErroredAsync(PullRequestWebhookEvent webhookEvent, string errorMessage)
    {
        webhookEvent.ErrorMessage = errorMessage;
        webhookEvent.ErroredAt = DateTime.UtcNow;
        await pullRequestWebhookEventRepository.UpdateAsync(webhookEvent);
    }

    private static async Task HandlePrOpenedOrSynchronizeAsync(
        Octokit.Webhooks.Events.PullRequestEvent prEvent,
        ProjectId projectId,
        IProjectEnvironmentRepository environmentRepository,
        IProjectRepository projectRepository,
        IMediator mediator)
    {
        var branchName = prEvent.PullRequest.Head.Ref;

        // Look for an existing environment matching the branch
        var existing = await environmentRepository.GetProjectEnvironmentByBranchAsync(
            projectId, branchName);

        if (existing is null)
        {
            Log.Information(
                "No environment exists for branch {Branch} on project {ProjectId}. " +
                "Environments must be created manually with a billing tier.",
                branchName, projectId);
            return;
        }

        Log.Information(
            "Found existing environment {EnvironmentId} for branch {Branch} on project {ProjectId}",
            existing.Id, branchName, projectId);

        // Trigger deployment for existing environment if auto-deploy is on
        if (existing.AutoDeploy && prEvent.PullRequest.Head.Sha is { } commitSha)
        {
            await TriggerDeploymentAsync(mediator, projectId, existing.Id, prEvent, branchName, commitSha);
        }
    }

    private static async Task HandlePrClosedAsync(
        Octokit.Webhooks.Events.PullRequestEvent prEvent,
        ProjectId projectId,
        IProjectEnvironmentRepository environmentRepository,
        IMediator mediator)
    {
        var branchName = prEvent.PullRequest.Head.Ref;

        var environment = await environmentRepository.GetProjectEnvironmentByBranchAsync(
            projectId, branchName);

        if (environment is null || environment.Type != EnvironmentType.Preview)
        {
            Log.Information(
                "No preview environment found for closed PR branch {Branch} on project {ProjectId}",
                branchName, projectId);
            return;
        }

        // Delete via command handler to trigger Stripe cancellation and VM cleanup
        await mediator.Send(new DeleteEnvironmentCommand(projectId, environment.Id));

        Log.Information(
            "Deleted preview environment {EnvironmentId} for closed PR on project {ProjectId}",
            environment.Id, projectId);
    }

    private static async Task TriggerDeploymentAsync(
        IMediator mediator,
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        Octokit.Webhooks.Events.PullRequestEvent prEvent,
        string branchName,
        string commitSha)
    {
        var deployCommand = new DeployProjectCommand(
            projectId,
            CommitHash: commitSha,
            CommitMessage: $"PR #{prEvent.PullRequest.Number}: {prEvent.PullRequest.Title}",
            CommitAuthor: prEvent.PullRequest.User.Login,
            CommitBranch: branchName,
            CommitEmail: prEvent.PullRequest.User.Login + "@users.noreply.github.com",
            OverrideAuthorization: true,
            EnvironmentId: environmentId);

        await mediator.Send(deployCommand);
    }
}
