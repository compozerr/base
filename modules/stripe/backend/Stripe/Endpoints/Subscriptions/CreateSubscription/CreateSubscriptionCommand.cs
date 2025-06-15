using Core.MediatR;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed record CreateSubscriptionCommand(
	string PriceId) : ICommand<CreateSubscriptionResponse>;
