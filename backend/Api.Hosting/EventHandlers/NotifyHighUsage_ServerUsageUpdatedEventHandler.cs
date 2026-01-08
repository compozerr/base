using Api.Hosting.Events;
using Core.Abstractions;
using Serilog;

namespace Api.Hosting.EventHandlers;

public sealed class NotifyHighUsage_ServerUsageUpdatedEventHandler
    : EntityDomainEventHandlerBase<ServerUsageUpdatedEvent>
{
    private const decimal HighUsageThreshold = 0.8m;

    protected override Task HandleAfterSaveAsync(ServerUsageUpdatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var server = domainEvent.Entity;
        var usage = server.Usage;

        var highUsageMetrics = new List<string>();

        if (usage.AvgCpuPercentage > HighUsageThreshold)
        {
            highUsageMetrics.Add($"CPU: {usage.AvgCpuPercentage:P1}");
        }

        if (usage.AvgRamPercentage > HighUsageThreshold)
        {
            highUsageMetrics.Add($"RAM: {usage.AvgRamPercentage:P1}");
        }

        if (usage.AvgDiskPercentage > HighUsageThreshold)
        {
            highUsageMetrics.Add($"Disk: {usage.AvgDiskPercentage:P1}");
        }

        if (highUsageMetrics.Count > 0)
        {
            Log.Logger.Error(
                "High server usage detected on server {ServerId} ({ServerHostName}): {HighUsageMetrics}",
                server.Id,
                server.HostName,
                string.Join(", ", highUsageMetrics));
        }

        return Task.CompletedTask;
    }
}
