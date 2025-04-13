using Jobs;

namespace Api.Hosting.Jobs;

public static class HostingJobsExtensions
{
    public static void AddHostingJobs(this IBackgroundJobManager backgroundJobManager)
    {
        Console.WriteLine("Adding hosting jobs");
        backgroundJobManager.RecurringJob(
            () => Console.WriteLine("Hi " + DateTime.Now.ToLongTimeString()), Cron.Minutely());
    }
}