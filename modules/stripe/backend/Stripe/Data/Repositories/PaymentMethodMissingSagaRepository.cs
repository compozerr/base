using Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Jobs.SagaJobs;

namespace Stripe.Data.Repositories;

public interface IPaymentMethodMissingSagaRepository :
    IGenericRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, StripeDbContext>,
    ISagaRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, PaymentMethodMissingSagaAuditLog>
{
    Task<PaymentMethodMissingSaga?> GetActiveSagaForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
}

public sealed class PaymentMethodMissingSagaRepository(
    StripeDbContext context) : GenericRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, StripeDbContext>(context), IPaymentMethodMissingSagaRepository
{
    public Task<PaymentMethodMissingSaga?> GetActiveSagaForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return Query()
            .Where(s => s.CustomerId == customerId && s.Status == PaymentMethodMissingSagaStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentMethodMissingSagaAuditLog> AddAuditLogAsync(PaymentMethodMissingSagaAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await context.PaymentMethodMissingSagaAuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public Task<PaymentMethodMissingSaga?> GetByIdAsync(PaymentMethodMissingSagaId id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, cancellationToken);
    }

    Task<PaymentMethodMissingSaga> ISagaRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, PaymentMethodMissingSagaAuditLog>.UpdateAsync(PaymentMethodMissingSaga entity, CancellationToken cancellationToken)
    {
        return (Task<PaymentMethodMissingSaga>)UpdateAsync(entity, cancellationToken);
    }
}
