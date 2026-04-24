using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed class GetInvoicesCommandValidator : AbstractValidator<GetInvoicesCommand>
{
	public GetInvoicesCommandValidator()
	{
		RuleFor(x => x.Limit).InclusiveBetween(1, 100);
	}
}
