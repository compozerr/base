using Api.Abstractions;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public sealed record SubscriptionDto(
    string Id,
    ProjectId ProjectId,
    string Name,
    string Status,
    string PlanId,
    string ServerTierId,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    decimal Amount,
    string Currency)
{
    public static SubscriptionDto FromSubscription(Subscription subscription, bool isProduction)
    {
        return new SubscriptionDto(
            Id: subscription.Id,
            ProjectId: ProjectId.Create(Guid.Parse(subscription.Metadata["project_id"])),
            Name: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Product?.Name ?? "Subscription",
            Status: subscription.Status,
            PlanId: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "",
            ServerTierId: Prices.GetInternalId(subscription.Items?.Data?.FirstOrDefault()?.Plan?.Id ?? "", isProduction),
            CurrentPeriodStart: subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodStart ?? DateTime.UtcNow,
            CurrentPeriodEnd: subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd ?? DateTime.UtcNow,
            CancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
            Amount: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Amount / 100m ?? 0,
            Currency: subscription.Items?.Data?.FirstOrDefault()?.Plan?.Currency?.ToUpper() ?? "USD"
        );
    }
};

public sealed record GetUserSubscriptionsResponse(
    List<SubscriptionDto> Subscriptions);
