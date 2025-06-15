using Core.MediatR;
using Api.Abstractions;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed record UpdateSubscriptionCommand(
	string SubscriptionId,
	ServerTierId NewTierID,
	bool IsUpgrade) : ICommand<UpdateSubscriptionResponse>;
