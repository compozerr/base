using Core.Extensions;
using Github.Abstractions;
using Github.Models;
using Github.Repositories;
using Jobs;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Github.Jobs;

public sealed class PushWebhookProcessorJob(
    IPushWebhookEventRepository pushWebhookEventRepository) : JobBase<PushWebhookProcessorJob, PushWebhookEventId>
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

            

            await MarkAsHandledAsync(pushWebhookEvent);
        }
        catch (Exception ex)
        {
            Log.ForContext(nameof(ex), ex)
               .ForContext(nameof(pushWebhookEvent), pushWebhookEvent.Id)
               .Error("Error processing PushWebhookEvent {PushWebhookEventId}", pushWebhookEvent.Id);

            await MarkAsErroredAsync(pushWebhookEvent, ex.Message);
        }
    }

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