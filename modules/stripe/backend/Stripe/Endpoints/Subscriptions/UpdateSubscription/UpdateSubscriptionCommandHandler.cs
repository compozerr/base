using Core.MediatR;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed class UpdateSubscriptionCommandHandler(
	/* Dependencies go here */) : ICommandHandler<UpdateSubscriptionCommand, UpdateSubscriptionResponse>
{
	public async Task<UpdateSubscriptionResponse> Handle(UpdateSubscriptionCommand command, CancellationToken cancellationToken = default)
	{
		// Implementation goes here
		return new UpdateSubscriptionResponse();
	}
}
