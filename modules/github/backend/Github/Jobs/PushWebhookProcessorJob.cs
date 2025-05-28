using Core.Extensions;
using Github.Abstractions;
using Github.Models;
using Github.Repositories;
using Jobs;
using Microsoft.EntityFrameworkCore;

namespace Github.Jobs;

public sealed class PushWebhookProcessorJob(
    IPushWebhookEventRepository pushWebhookEventRepository) : JobBase<PushWebhookProcessorJob, PushWebhookEventId>
{
    public override async Task ExecuteAsync(PushWebhookEventId pushWebhookEventId)
    {
        var events = await pushWebhookEventRepository.GetByIdAsync(pushWebhookEventId);

        if (events is null or { HandledAt: not null } or { ErroredAt: not null })
        {
            // If the event is already handled or errored, we skip processing
            return;
        }

        await HandleEventAsync(events);
    }

    private async Task HandleEventAsync(
        PushWebhookEvent pushWebhookEvent)
    {
        try
        {
            // Simulate processing the event
            Console.WriteLine($"Handling Push Event: {pushWebhookEvent.Event}");

            // Mark as handled
            pushWebhookEvent.HandledAt = DateTime.UtcNow;
            await pushWebhookEventRepository.UpdateAsync(pushWebhookEvent);
        }
        catch (Exception ex)
        {
            // Log the error and mark as errored
            Console.WriteLine($"Error processing Push Event: {ex.Message}");
            pushWebhookEvent.ErrorMessage = ex.Message;
            pushWebhookEvent.ErroredAt = DateTime.UtcNow;
            await pushWebhookEventRepository.UpdateAsync(pushWebhookEvent);
        }
    }
}