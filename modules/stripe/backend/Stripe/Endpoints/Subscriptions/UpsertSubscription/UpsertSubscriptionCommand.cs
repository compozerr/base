using Core.MediatR;
using Api.Abstractions;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed record UpsertSubscriptionCommand(
    ProjectId ProjectId,
    ServerTierId ServerTierId,
    string? CouponCode = null,
    bool OverrideAuthorization = false) : ICommand<UpsertSubscriptionResponse>;
