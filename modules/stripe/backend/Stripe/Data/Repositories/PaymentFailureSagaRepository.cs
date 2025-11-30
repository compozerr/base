using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Jobs.SagaJobs;

namespace Stripe.Data.Repositories;

public interface IPaymentFailureSagaRepository :
    IGenericRepository<PaymentFailureSaga, PaymentFailureSagaId, StripeDbContext>,
    ISagaRepository<PaymentFailureSaga, PaymentFailureSagaId, PaymentFailureSagaAuditLog>
{
    Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
}

public sealed class PaymentFailureSagaRepository(
    StripeDbContext context) : GenericRepository<PaymentFailureSaga, PaymentFailureSagaId, StripeDbContext>(context), IPaymentFailureSagaRepository
{
    private readonly StripeDbContext Context = context;

    public Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        return Query()
            .Where(s => s.SubscriptionId == subscriptionId && s.Status == PaymentFailureSagaStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentFailureSagaAuditLog> AddAuditLogAsync(PaymentFailureSagaAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await Context.PaymentFailureSagaAuditLogs.AddAsync(auditLog, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<PaymentFailureSaga?> GetByIdAsync(PaymentFailureSagaId id, CancellationToken cancellationToken = default)
    {
        return await base.GetByIdAsync(id, cancellationToken);
    }

    Task<PaymentFailureSaga> ISagaRepository<PaymentFailureSaga, PaymentFailureSagaId, PaymentFailureSagaAuditLog>.UpdateAsync(PaymentFailureSaga entity, CancellationToken cancellationToken)
    {
        return (Task<PaymentFailureSaga>)base.UpdateAsync(entity, cancellationToken);
    }
}
