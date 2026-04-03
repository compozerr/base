using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvironmentBillingStatus
{
    Active = 0,
    Trialing = 1,
    Suspended = 2,
    Cancelled = 3,
}

public static class EnvironmentBillingStatusExtensions
{
    public static EnvironmentBillingStatus FromStripeStatus(string stripeStatus) => stripeStatus switch
    {
        "trialing" => EnvironmentBillingStatus.Trialing,
        "active" => EnvironmentBillingStatus.Active,
        "past_due" or "unpaid" => EnvironmentBillingStatus.Suspended,
        "canceled" or "incomplete_expired" => EnvironmentBillingStatus.Cancelled,
        _ => EnvironmentBillingStatus.Active,
    };
}
