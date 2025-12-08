using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Stripe.Extensions;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public sealed class UpsertSubscriptionCommandValidator : AbstractValidator<UpsertSubscriptionCommand>
{
    public UpsertSubscriptionCommandValidator(IServiceScopeFactory scopeFactory)
    {
        RuleFor(x => x)
            .UserMustHavePaymentMethod(scopeFactory)
            .When(x => !x.OverrideAuthorization);
    }
}
