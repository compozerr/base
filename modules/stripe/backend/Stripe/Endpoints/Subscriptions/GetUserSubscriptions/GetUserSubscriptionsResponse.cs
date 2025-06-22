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
    string Currency);

public sealed record GetUserSubscriptionsResponse(
    List<SubscriptionDto> Subscriptions);
