using Core.Abstractions;

namespace Stripe.Abstractions;

public sealed record PaymentMethodMissingSagaId : IdBase<PaymentMethodMissingSagaId>, IId<PaymentMethodMissingSagaId>
{
    public PaymentMethodMissingSagaId(Guid value) : base(value)
    {
    }

    public static PaymentMethodMissingSagaId Create(Guid value)
        => new(value);
}
