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
    private readonly StripeDbContext Context = context;
    public Task<PaymentMethodMissingSaga?> GetActiveSagaForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        return Query()
            .Where(s => s.CustomerId == customerId && s.Status == PaymentMethodMissingSagaStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentMethodMissingSagaAuditLog> AddAuditLogAsync(PaymentMethodMissingSagaAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await Context.PaymentMethodMissingSagaAuditLogs.AddAsync(auditLog, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<PaymentMethodMissingSaga?> GetByIdAsync(PaymentMethodMissingSagaId id, CancellationToken cancellationToken = default)
    {
        return await base.GetByIdAsync(id, cancellationToken);
    }

    Task<PaymentMethodMissingSaga> ISagaRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, PaymentMethodMissingSagaAuditLog>.UpdateAsync(PaymentMethodMissingSaga entity, CancellationToken cancellationToken)
    {
        return (Task<PaymentMethodMissingSaga>)base.UpdateAsync(entity, cancellationToken);
    }
}
