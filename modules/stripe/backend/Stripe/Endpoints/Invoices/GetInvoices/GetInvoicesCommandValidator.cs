using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed class GetInvoicesCommandValidator : AbstractValidator<GetInvoicesCommand>
{
	public GetInvoicesCommandValidator()
	{
		// Add validation rules using RuleFor()
	}
}
