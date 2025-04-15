using System.Linq.Expressions;
using Hangfire;

namespace Jobs;


public interface IBackgroundJobManager
{
    void RecurringJob(Expression<Action<IServiceProvider>> Action, Cron When);
    void RecurringJob(Expression<Func<IServiceProvider, Task>> Action, Cron When);
}

public class BackgroundJobManager(IRecurringJobManager recurringJobManager) : IBackgroundJobManager
{
    public void RecurringJob(Expression<Action<IServiceProvider>> Action, Cron When)
    {
        recurringJobManager.AddOrUpdate(
            GetJobId(Action.Body),
            CreateExpression(Action),
            When.Value);
    }

    public void RecurringJob(Expression<Func<IServiceProvider, Task>> Action, Cron When)
    {
        recurringJobManager.AddOrUpdate(
            GetJobId(Action.Body),
            CreateExpression(Action),
            When.Value);
    }

    private Expression<Action> CreateExpression<TDelegate>(Expression<TDelegate> action)
        where TDelegate : Delegate
    {
        var methodCall = action.Body as MethodCallExpression;
        var method = methodCall?.Method ?? 
            (action.Body is MemberExpression memberExpr ? memberExpr.Member as System.Reflection.MethodInfo : null) ?? 
            ((LambdaExpression)action).Compile().Method;
            
        var parameters = method.GetParameters();
        var parameterExpressions = parameters.Select(param => Expression.Parameter(param.ParameterType)).ToArray();
        var call = Expression.Call(Expression.Constant(this), method, parameterExpressions);
        return Expression.Lambda<Action>(call, parameterExpressions);
    }

    private static string GetJobId(Expression? expression)
        => expression is MethodCallExpression methodCall ? methodCall.Method.Name : Guid.NewGuid().ToString();
}
