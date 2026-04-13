using Auth.Models;
using Organizations.Models;
using Stripe.Data.Models;
using Stripe.Events;

namespace Api.EventHandlers.Stripe.PaymentMethodMissingActions.Core;

public interface IPaymentMethodMissingAction
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}

public abstract class BasePaymentMethodMissingAction(
    PaymentMethodMissingEvent @event,
    User user,
    Organization organization,
    IServiceProvider serviceProvider) : IPaymentMethodMissingAction
{
    protected readonly PaymentMethodMissingEvent Event = @event;
    protected readonly User User = user;
    protected readonly Organization Organization = organization;
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly PaymentMethodMissingSaga Saga = @event.Saga;

    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the remaining days until termination based on the saga start time and total days.
    /// </summary>
    protected int GetRemainingDaysUntilTermination()
    {
        var daysSinceStart = (DateTime.UtcNow - Saga.StartedAtUtc).Days;
        var remainingDays = Saga.DaysUntilTermination - daysSinceStart;
        return Math.Max(0, remainingDays);
    }

    /// <summary>
    /// Returns a human-readable label for the organization context.
    /// </summary>
    protected string GetOrgDisplayLabel()
    {
        return Organization.IsPersonal
            ? "your personal account"
            : $"your organization \"{Organization.Name}\"";
    }

    /// <summary>
    /// Builds a settings path with the given query string, including the org ID.
    /// </summary>
    protected string GetSettingsPath(string queryString)
    {
        return $"settings?{queryString}";
    }
}