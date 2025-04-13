using System.Linq.Expressions;

namespace Jobs;

public static class BackgroundJobManager
{
    public static void RecurringJob(Expression<Action> Action, Cron When)
    {
        var methodName = ((MethodCallExpression)Action.Body).Method.Name;
        var recurringJobId = methodName;

        Hangfire.RecurringJob.AddOrUpdate(
            recurringJobId,
            Action,
            When.Value);
    }
}
