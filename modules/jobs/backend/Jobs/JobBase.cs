using Hangfire;

namespace Jobs;

public abstract class JobBase<T> where T : JobBase<T>
{
    public abstract Task ExecuteAsync();

    public static void RegisterJob(Cron when, IRecurringJobManager recurringJobManager)
    {
        var jobId = typeof(T).Name;
        recurringJobManager.AddOrUpdate<T>(
            jobId,
            x => x.ExecuteAsync(),
            when.Value);
    }

    public static void Enqueue()
    {
        BackgroundJob.Enqueue<T>(job => job.ExecuteAsync());
    }
}

public abstract class JobBase<T, TArg> where T : JobBase<T, TArg>
{
    public abstract Task ExecuteAsync(TArg arg);

    public static void RegisterJob(Cron when, IRecurringJobManager recurringJobManager, TArg arg)
    {
        var jobId = typeof(T).Name;
        recurringJobManager.AddOrUpdate<T>(
            jobId,
            x => x.ExecuteAsync(arg),
            when.Value);
    }

    public static void Enqueue(TArg arg)
    {
        BackgroundJob.Enqueue<T>(job => job.ExecuteAsync(arg));
    }
}