using Core.Abstractions;

namespace Stripe.Abstractions;

public sealed record PaymentFailureSagaAuditLogId : IdBase<PaymentFailureSagaAuditLogId>, IId<PaymentFailureSagaAuditLogId>
{
    public PaymentFailureSagaAuditLogId(Guid value) : base(value)
    {
    }

    public static PaymentFailureSagaAuditLogId Create(Guid value)
        => new(value);
}
