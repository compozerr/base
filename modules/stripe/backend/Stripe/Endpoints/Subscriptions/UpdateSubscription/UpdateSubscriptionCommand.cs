using Core.MediatR;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed record UpdateSubscriptionCommand(
	string SubscriptionId) : ICommand<UpdateSubscriptionResponse>;
