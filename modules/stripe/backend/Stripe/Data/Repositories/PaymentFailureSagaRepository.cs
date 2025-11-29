using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe.Abstractions;
using Stripe.Data.Models;

namespace Stripe.Data.Repositories;

public interface IPaymentFailureSagaRepository : IGenericRepository<PaymentFailureSaga, PaymentFailureSagaId, StripeDbContext>
{
    Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<PaymentFailureSagaAuditLog> AddAuditLogAsync(PaymentFailureSagaAuditLog auditLog, CancellationToken cancellationToken = default);
}

public sealed class PaymentFailureSagaRepository(
    StripeDbContext context) : GenericRepository<PaymentFailureSaga, PaymentFailureSagaId, StripeDbContext>(context), IPaymentFailureSagaRepository
{
    public Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        return Query()
            .Where(s => s.SubscriptionId == subscriptionId && s.Status == PaymentFailureSagaStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentFailureSagaAuditLog> AddAuditLogAsync(PaymentFailureSagaAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await context.PaymentFailureSagaAuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }
}
