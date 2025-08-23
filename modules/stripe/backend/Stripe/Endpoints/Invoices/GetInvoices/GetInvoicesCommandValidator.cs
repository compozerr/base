using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe.Endpoints.PaymentMethods.GetInvoices;

public sealed class GetInvoicesCommandValidator : AbstractValidator<GetInvoicesCommand>
{
	public GetInvoicesCommandValidator(IServiceScopeFactory scopeFactory)
	{
		var scope = scopeFactory.CreateScope();
		// Add required services using scope.ServiceProvider.GetRequiredService<T>()

		// Add validation rules using RuleFor()
	}
}
