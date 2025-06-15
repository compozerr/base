using Core.MediatR;
using Core.Services;
using Api.Abstractions;
using Stripe.Options;
using System.Collections.Generic;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
	IFrontendLocation frontendLocation) : ICommandHandler<CreateSubscriptionCommand, CreateSubscriptionResponse>
{
	public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken = default)
	{
		// Get the server tier to determine the price
		var serverTier = Api.Abstractions.ServerTiers.GetById(command.ServerTierId);
		
		// Create metadata to track the server and tier
		var metadata = new Dictionary<string, string>
		{
			{ "ServerId", command.ServerId.Value },
			{ "ServerTierId", command.ServerTierId.Value },
			{ "RamGB", serverTier.RamGb.ToString() },
			{ "Cores", serverTier.Cores.ToString() }
		};

		// Convert the price to Stripe's expected format (cents)
		var priceCents = (long)(serverTier.Price.Amount * 100);
		
		// Create a price object for this specific tier
		var priceOptions = new Stripe.PriceCreateOptions
		{
			UnitAmount = priceCents,
			Currency = serverTier.Price.Currency.ToLower(),
			Recurring = new Stripe.PriceRecurringOptions
			{
				Interval = "month"
			},
			ProductData = new Stripe.PriceProductDataOptions
			{
				Name = $"Server Tier {command.ServerTierId.Value}",
				Metadata = metadata
			},
			Metadata = metadata
		};
		
		var priceService = new Stripe.PriceService();
		var price = await priceService.CreateAsync(priceOptions, null, cancellationToken);
		
		var options = new Stripe.Checkout.SessionCreateOptions
		{
			Mode = "subscription",
			LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
			{
				new Stripe.Checkout.SessionLineItemOptions
				{
					Price = price.Id,
					Quantity = 1,
				},
			},
			SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
			{
				Metadata = metadata
			},
			UiMode = "embedded",
			Metadata = metadata,
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
