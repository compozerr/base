using Core.MediatR;
using Api.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed class UpdateSubscriptionCommandHandler : ICommandHandler<UpdateSubscriptionCommand, UpdateSubscriptionResponse>
{
	public async Task<UpdateSubscriptionResponse> Handle(UpdateSubscriptionCommand command, CancellationToken cancellationToken = default)
	{
		// Get the server tier to determine the new price
		var newTier = Api.Abstractions.ServerTiers.GetById(command.NewTierID);
		
		// Create a Stripe price for the new tier
		var priceCents = (long)(newTier.Price.Amount * 100);
		
		// Create metadata for the new price
		var metadata = new Dictionary<string, string>
		{
			{ "ServerTierId", command.NewTierID.Value },
			{ "RamGB", newTier.RamGb.ToString() },
			{ "Cores", newTier.Cores.ToString() }
		};
		
		// Create a new price in Stripe
		var priceOptions = new Stripe.PriceCreateOptions
		{
			UnitAmount = priceCents,
			Currency = newTier.Price.Currency.ToLower(),
			Recurring = new Stripe.PriceRecurringOptions
			{
				Interval = "month"
			},
			ProductData = new Stripe.PriceProductDataOptions
			{
				Name = $"Server Tier {command.NewTierID.Value}",
				Metadata = metadata
			},
			Metadata = metadata
		};
		
		var priceService = new Stripe.PriceService();
		var price = await priceService.CreateAsync(priceOptions, null, cancellationToken);
		
		// Get the subscription from Stripe
		var subscriptionService = new Stripe.SubscriptionService();
		var subscription = await subscriptionService.GetAsync(command.SubscriptionId, null, null, cancellationToken);
		
		// Create subscription update options
		var options = new Stripe.SubscriptionUpdateOptions
		{
			CancelAtPeriodEnd = false,
			ProrationBehavior = command.IsUpgrade 
				? "create_prorations" // Upgrade: charge immediately for the difference
				: "none",            // Downgrade: apply at end of billing cycle
			Items = new List<Stripe.SubscriptionItemOptions>
			{
				new Stripe.SubscriptionItemOptions
				{
					Id = subscription.Items.Data[0].Id,
					Price = price.Id,
				}
			},
			Metadata = metadata
		};
		
		// Update the subscription
		var updatedSubscription = await subscriptionService.UpdateAsync(command.SubscriptionId, options, null, cancellationToken);
		
		// Calculate proration if it's an upgrade
		decimal prorationAmount = 0;
		if (command.IsUpgrade && updatedSubscription.LatestInvoice != null)
		{
			// Get invoice to calculate proration
			var invoiceService = new Stripe.InvoiceService();
			var invoice = await invoiceService.GetAsync(updatedSubscription.LatestInvoice, null, null, cancellationToken);
			prorationAmount = invoice.AmountDue / 100m; // Convert from cents to dollars
		}
		
		// Format next billing date
		string? nextBillingDate = null;
		if (updatedSubscription.CurrentPeriodEnd.HasValue)
		{
			nextBillingDate = updatedSubscription.CurrentPeriodEnd.Value.ToString("yyyy-MM-dd");
		}
		
		return new UpdateSubscriptionResponse(
			updatedSubscription.Id,
			updatedSubscription.Status,
			prorationAmount,
			nextBillingDate,
			command.IsUpgrade);
	}
}
