using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Jobs;

public static class WebApplicationExtensions
{
    public static void AddRecurringJob<T>(this WebApplication app, Cron When) where T : JobBase<T>
    {
        using var scope = app.Services.CreateScope();

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        JobBase<T>.RegisterJob(
            When,
            recurringJobManager);
    }
}