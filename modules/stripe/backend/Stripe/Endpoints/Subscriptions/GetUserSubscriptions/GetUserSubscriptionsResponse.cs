using Api.Abstractions;
using Stripe.Services;

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
    Money Amount,
    Money? OriginalAmount = null,
    string? CouponCode = null)
{
    public static SubscriptionDto FromSubscription(Subscription subscription, bool isProduction)
    {
        var item = subscription.Items?.Data?.FirstOrDefault();
        var baseAmount = item?.Plan?.AmountDecimal ?? 0;
        var currency = item?.Plan?.Currency ?? "usd";

        // Get the first active discount/coupon
        // Stripe uses Discounts collection (can have multiple discounts)
        var discount = subscription.Discounts?.FirstOrDefault();
        var coupon = discount?.Coupon;
        var couponCode = coupon?.Id;
        decimal actualAmount = baseAmount;

        if (coupon != null)
        {
            if (coupon.AmountOff.HasValue)
            {
                // Fixed amount discount (e.g., $3 off)
                actualAmount = baseAmount - coupon.AmountOff.Value;
            }
            else if (coupon.PercentOff.HasValue)
            {
                // Percentage discount (e.g., 25% off)
                actualAmount = baseAmount * (1 - (coupon.PercentOff.Value / 100m));
            }

            // Ensure amount doesn't go below 0
            actualAmount = Math.Max(0, actualAmount);
        }

        return new SubscriptionDto(
            Id: subscription.Id,
            ProjectId: ProjectId.Create(Guid.Parse(subscription.Metadata["project_id"])),
            Name: item?.Plan?.Product?.Name ?? "Subscription",
            Status: subscription.Status,
            PlanId: item?.Plan?.Id ?? "",
            ServerTierId: Prices.GetInternalId(item?.Plan?.Id ?? "", isProduction),
            CurrentPeriodStart: item?.CurrentPeriodStart ?? DateTime.UtcNow,
            CurrentPeriodEnd: item?.CurrentPeriodEnd ?? DateTime.UtcNow,
            CancelAtPeriodEnd: subscription.CancelAtPeriodEnd,
            Amount: new Money(
                amount: actualAmount * 0.01m,
                currency: currency
            ),
            OriginalAmount: coupon != null ? new Money(amount: baseAmount * 0.01m, currency: currency) : null,
            CouponCode: couponCode
        );
    }
};

public sealed record GetUserSubscriptionsResponse(
    List<SubscriptionDto> Subscriptions);
