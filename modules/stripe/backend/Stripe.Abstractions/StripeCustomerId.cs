using Core.Abstractions;

namespace Stripe.Abstractions;

public sealed record StripeCustomerId : IdBase<StripeCustomerId>, IId<StripeCustomerId>
{
    public StripeCustomerId(Guid value) : base(value)
    {
    }

    public static StripeCustomerId Create(Guid value)
        => new(value);
}
