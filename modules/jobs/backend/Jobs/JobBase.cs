using Hangfire;

namespace Jobs;

public abstract class JobBase<T> where T : JobBase<T>
{
    public abstract Task ExecuteAsync();

    /// <summary>
    /// Optional: Override to provide a distributed lock key for this job execution.
    /// When implemented, the job will only execute if the lock can be acquired.
    /// This prevents duplicate execution when multiple job instances are queued.
    /// </summary>
    public virtual string? GetDistributedLockKey() => null;

    /// <summary>
    /// Optional: Override to specify the lock timeout. Default is 5 minutes.
    /// </summary>
    public virtual TimeSpan GetDistributedLockTimeout() => TimeSpan.FromMinutes(5);

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

    /// <summary>
    /// Optional: Override to provide a distributed lock key for this job execution.
    /// When implemented, the job will only execute if the lock can be acquired.
    /// This prevents duplicate execution when multiple job instances are queued.
    /// The argument can be used to generate a unique lock key per argument.
    /// </summary>
    public virtual string? GetDistributedLockKey(TArg arg) => null;

    /// <summary>
    /// Optional: Override to specify the lock timeout. Default is 5 minutes.
    /// </summary>
    public virtual TimeSpan GetDistributedLockTimeout() => TimeSpan.FromMinutes(5);

    /// <summary>
    /// Public wrapper that handles distributed locking if configured.
    /// This must be public for Hangfire to invoke it in the background.
    /// </summary>
    public async Task ExecuteWithLockAsync(TArg arg)
    {
        var lockKey = GetDistributedLockKey(arg);

        if (string.IsNullOrEmpty(lockKey))
        {
            // No locking configured, execute directly
            await ExecuteAsync(arg);
            return;
        }

        // Use Hangfire's distributed lock
        using var connection = JobStorage.Current.GetConnection();
        using var distributedLock = connection.AcquireDistributedLock(lockKey, GetDistributedLockTimeout());

        await ExecuteAsync(arg);
    }

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
        BackgroundJob.Enqueue<T>(job => job.ExecuteWithLockAsync(arg));
    }
}