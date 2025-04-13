using System.Linq.Expressions;
using Hangfire;

namespace Jobs;


public interface IBackgroundJobManager
{
    void RecurringJob(Expression<Action<IServiceProvider>> Action, Cron When);
}

public class BackgroundJobManager(IRecurringJobManager recurringJobManager) : IBackgroundJobManager
{
    public void RecurringJob(Expression<Action<IServiceProvider>> Action, Cron When)
    {
        var methodName = ((MethodCallExpression)Action.Body).Method.Name;
        var recurringJobId = methodName;

        recurringJobManager.AddOrUpdate(
            recurringJobId,
            Action,
            When.Value);
    }
}
