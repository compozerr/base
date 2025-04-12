using System.Linq.Expressions;

namespace Jobs;

public static class BackgroundJobManager
{
    public static void RecurringJob(Expression<Action> Action, Cron When)
    {
        var jobId = BackgruRecurringJob(Action, When);
        Console.WriteLine($"Enqueued job with ID: {jobId}");
    }
}
