using Core.Abstractions;

namespace Stripe.Abstractions;

public sealed record PaymentFailureSagaId : IdBase<PaymentFailureSagaId>, IId<PaymentFailureSagaId>
{
    public PaymentFailureSagaId(Guid value) : base(value)
    {
    }

    public static PaymentFailureSagaId Create(Guid value)
        => new(value);
}
