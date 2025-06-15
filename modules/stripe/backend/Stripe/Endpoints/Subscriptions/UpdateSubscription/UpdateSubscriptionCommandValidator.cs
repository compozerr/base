using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe.Endpoints.UpdateSubscription;

public sealed class UpdateSubscriptionCommandValidator : AbstractValidator<UpdateSubscriptionCommand>
{
	public UpdateSubscriptionCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}
