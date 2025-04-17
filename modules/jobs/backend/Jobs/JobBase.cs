using Hangfire;

namespace Jobs;

public abstract class JobBase<T> where T : JobBase<T>
{
    public abstract Task ExecuteAsync();

    public static void RegisterJob(Cron when, IRecurringJobManager recurringJobManager)
    {
        var jobId = typeof(T).Name;
        // recurringJobManager.RemoveIfExists(jobId);
        recurringJobManager.AddOrUpdate<T>(
            jobId,
            x => x.ExecuteAsync(),
            when.Value);
    }
}