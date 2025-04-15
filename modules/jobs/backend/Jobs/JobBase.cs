using Hangfire;

namespace Jobs;

public abstract class JobBase<T> where T : JobBase<T>
{
    public abstract Task ExecuteAsync();

    public static void RegisterJob(Cron when)
    {
        RecurringJob.AddOrUpdate<T>(
            typeof(T).Name,
            x => x.ExecuteAsync(),
            when.Value);
    }
}