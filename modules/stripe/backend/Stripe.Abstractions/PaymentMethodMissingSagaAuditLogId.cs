using Core.Abstractions;

namespace Stripe.Abstractions;

public sealed record PaymentMethodMissingSagaAuditLogId : IdBase<PaymentMethodMissingSagaAuditLogId>, IId<PaymentMethodMissingSagaAuditLogId>
{
    public PaymentMethodMissingSagaAuditLogId(Guid value) : base(value)
    {
    }

    public static PaymentMethodMissingSagaAuditLogId Create(Guid value)
        => new(value);
}
