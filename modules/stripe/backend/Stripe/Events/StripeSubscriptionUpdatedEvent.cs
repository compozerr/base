using Api.Abstractions;
using Core.Abstractions;

namespace Stripe.Events;

public record StripeSubscriptionUpdatedEvent(
    string SubscriptionId,
    ProjectId ProjectId,
    ServerTierId ServerTierId) : IEvent;