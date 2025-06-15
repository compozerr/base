using Core.MediatR;
using Core.Services;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
	IFrontendLocation frontendLocation) : ICommandHandler<CreateSubscriptionCommand, CreateSubscriptionResponse>
{
	public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken = default)
	{
		// Implementation goes here
		var options = new Stripe.Checkout.SessionCreateOptions
		{
			Mode = "subscription",
			LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
	{
		new Stripe.Checkout.SessionLineItemOptions
		{
			Price = command.PriceId,
			Quantity = 1,
		},
	},
			UiMode = "embedded",
			ReturnUrl = frontendLocation.GetFromPath("checkout/return").ToString() + "?session_id={CHECKOUT_SESSION_ID}",
		};
		var service = new Stripe.Checkout.SessionService();
		Stripe.Checkout.Session session = await service.CreateAsync(options, null, cancellationToken);

		return new CreateSubscriptionResponse(
			session.Id,
			session.Url,
			session.ClientReferenceId,
			session.PaymentStatus);
	}
}
