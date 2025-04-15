using Hangfire;

namespace Jobs;

public abstract class JobBase<T> where T : JobBase<T>
{
    public abstract Task ExecuteAsync();

    public static void RegisterJob(Cron when, IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<T>(
            typeof(T).Name,
            x => x.ExecuteAsync(),
            when.Value);
    }
}