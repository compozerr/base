using Api.Abstractions;
using Github.Abstractions;
using Github.Models;
using Github.Repositories;
using Jobs;
using MediatR;
using Serilog;

namespace Github.Jobs;

public sealed class PushWebhookProcessorJob(
    IPushWebhookEventRepository pushWebhookEventRepository,
    IMediator mediator) : JobBase<PushWebhookProcessorJob, PushWebhookEventId>
{
    public override async Task ExecuteAsync(PushWebhookEventId pushWebhookEventId)
    {
        var @event = await pushWebhookEventRepository.GetByIdAsync(pushWebhookEventId);

        if (@event is null or { HandledAt: not null } or { ErroredAt: not null })
        {
            // If the event is already handled or errored, we skip processing
            return;
        }

        await HandleEventAsync(@event);
    }

    private async Task HandleEventAsync(
        PushWebhookEvent pushWebhookEvent)
    {
        try
        {
            Log.ForContext(nameof(pushWebhookEvent), pushWebhookEvent.Id)
               .Information("Processing PushWebhookEvent {PushWebhookEventId}", pushWebhookEvent.Id);

            if (Uri.TryCreate(pushWebhookEvent.Event.Repository?.CloneUrl, UriKind.Absolute, out var gitUrl) is false)
            {
                throw new InvalidOperationException("Invalid repository clone URL.");
            }

            var projectId = await pushWebhookEventRepository.GetProjectIdFromGitUrlAsync(
                gitUrl) ?? throw new InvalidOperationException("Project ID could not be determined from the repository URL.");


            if (pushWebhookEvent.Event.HeadCommit is
                {
                    Id: null or "",
                    Message: null or "",
                    Author: { Name: null or "", Email: null or "" }
                })
            {
                throw new InvalidOperationException("Head commit information is incomplete.");
            }

            var deployCommand = new DeployProjectCommand(
                projectId,
                CommitHash: pushWebhookEvent.Event.HeadCommit!.Id!,
                CommitMessage: pushWebhookEvent.Event.HeadCommit!.Message!,
                CommitAuthor: pushWebhookEvent.Event.HeadCommit!.Author!.Name!,
                CommitBranch: pushWebhookEvent.Event.Ref.Replace("refs/heads/", string.Empty),
                CommitEmail: pushWebhookEvent.Event.HeadCommit!.Author!.Email!,
                OverrideAuthorization: true);

            await mediator.Send(deployCommand);
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(ex), ex)
               .ForContext(nameof(pushWebhookEvent), pushWebhookEvent.Id)
               .Error("Error processing PushWebhookEvent {PushWebhookEventId}", pushWebhookEvent.Id);

            await MarkAsErroredAsync(pushWebhookEvent, ex.Message);
        }
        finally
        {
            await MarkAsHandledAsync(pushWebhookEvent);
        }
    }

    // private async Task<ProjectId> GetProjectIdAsync(
    //     string repoName)
    // {
    //     // Assuming the project ID is stored in the event, adjust as necessary
    //     return await pushWebhookEventRepository.GetProjectIdAsync(pushWebhookEvent);
    // }

    private async Task MarkAsHandledAsync(
        PushWebhookEvent pushWebhookEvent)
    {
        pushWebhookEvent.HandledAt = DateTime.UtcNow;
        await pushWebhookEventRepository.UpdateAsync(pushWebhookEvent);
    }

    private async Task MarkAsErroredAsync(
        PushWebhookEvent pushWebhookEvent, string errorMessage)
    {
        pushWebhookEvent.ErrorMessage = errorMessage;
        pushWebhookEvent.ErroredAt = DateTime.UtcNow;
        await pushWebhookEventRepository.UpdateAsync(pushWebhookEvent);
    }
}