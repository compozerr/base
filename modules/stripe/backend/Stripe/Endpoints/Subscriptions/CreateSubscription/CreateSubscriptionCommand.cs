using Core.MediatR;
using Api.Abstractions;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    ProjectId ProjectId,
    ServerTierId ServerTierId) : ICommand<CreateSubscriptionResponse>;
