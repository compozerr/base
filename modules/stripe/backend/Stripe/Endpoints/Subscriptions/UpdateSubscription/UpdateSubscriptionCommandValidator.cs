using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Api.Abstractions;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
{
	public UpdateSubscriptionCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		
		RuleFor(x => x.SubscriptionId)
			.NotEmpty().WithMessage("Subscription ID is required");
			
		RuleFor(x => x.NewTierID)
			.NotNull().WithMessage("New tier ID is required")
			.Must(BeValidServerTier).WithMessage("Must be a valid server tier");
	}
	
	private bool BeValidServerTier(ServerTierId tierId)
	{
		try
		{
			// Check if the tier exists
			var tier = ServerTiers.GetById(tierId);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
