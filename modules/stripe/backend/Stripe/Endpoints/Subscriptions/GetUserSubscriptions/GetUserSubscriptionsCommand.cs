using Core.MediatR;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public sealed record GetUserSubscriptionsCommand() : ICommand<GetUserSubscriptionsResponse>;
