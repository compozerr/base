using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

namespace Stripe.Extensions;

public static class StripeValidationExtensions
{
    public static IRuleBuilderOptions<T, T> UserMustHavePaymentMethod<T>(this IRuleBuilder<T, T> ruleBuilder, IServiceScopeFactory serviceScopeFactory)
    {
        return ruleBuilder.MustAsync(async (cmd, cancel) =>
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var userPaymentMethods = await mediator.Send(
                new GetUserPaymentMethodsCommand(),
                cancel);

            if (userPaymentMethods is null || userPaymentMethods.PaymentMethods.Count == 0)
                return false;

            return true;
        })
        .WithMessage("You haven't set up a payment method yet. Please add one to continue.")
        .WithName("PaymentMethod");
    }
}