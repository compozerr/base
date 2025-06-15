using Core.MediatR;
using Api.Abstractions;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    ServerId ServerId,
    ServerTierId ServerTierId) : ICommand<CreateSubscriptionResponse>;
