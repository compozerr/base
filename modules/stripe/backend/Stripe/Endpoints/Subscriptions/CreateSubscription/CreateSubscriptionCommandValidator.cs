using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
	public CreateSubscriptionCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}
