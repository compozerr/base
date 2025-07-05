using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.AttachPaymentMethod;

public sealed class AttachPaymentMethodCommandValidator : AbstractValidator<AttachPaymentMethodCommand>
{
    public AttachPaymentMethodCommandValidator(IServiceScopeFactory serviceScopeFactory)
    {
        var scope = serviceScopeFactory.CreateScope();

        var stripeService = scope.ServiceProvider.GetRequiredService<IStripeService>();

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .WithMessage("Payment method ID cannot be empty")
            .MaximumLength(255)
            .WithMessage("Payment method ID cannot be longer than 255 characters");
    }
}
