using Hangfire;
using Microsoft.Extensions.Logging;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Jobs.SagaJobs;

namespace Stripe.Services;

public interface IPaymentMethodMissingSagaOrchestrator
{
    Task<PaymentMethodMissingSaga?> StartSagaAsync(string customerId, CancellationToken cancellationToken = default);
    Task CancelSagaAsync(string customerId, PaymentMethodMissingSagaCancellationReason reason, CancellationToken cancellationToken = default);
    Task<PaymentMethodMissingSaga?> GetActiveSagaForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
}

public class PaymentMethodMissingSagaOrchestrator(
    IPaymentMethodMissingSagaRepository repository,
    ILogger<PaymentMethodMissingSagaOrchestrator> logger) : IPaymentMethodMissingSagaOrchestrator
{
    public async Task<PaymentMethodMissingSaga?> StartSagaAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var existingSaga = await repository.GetActiveSagaForCustomerAsync(customerId, cancellationToken);
        if (existingSaga != null)
        {
            logger.LogInformation(
                "Saga already exists for customer {CustomerId}, skipping creation",
                customerId);

            await LogAuditEventAsync(
                existingSaga.Id,
                PaymentMethodMissingSagaEvent.SagaStarted,
                null,
                cancellationToken);

            return null;
        }

        const int daysUntilTermination = 5;

        var saga = new PaymentMethodMissingSaga
        {
            CustomerId = customerId,
            Status = PaymentMethodMissingSagaStatus.Active,
            StartedAtUtc = DateTime.UtcNow,
            DaysUntilTermination = daysUntilTermination
        };

        // Schedule Hangfire delayed jobs
        saga.FirstWarningJobId = PaymentMethodMissingJob.Schedule(new(saga.Id, AttemptNumber: 1), TimeSpan.FromDays(1));
        saga.SecondWarningJobId = PaymentMethodMissingJob.Schedule(new(saga.Id, AttemptNumber: 2), TimeSpan.FromDays(3));
        saga.TerminationJobId = PaymentMethodMissingJob.Schedule(new(saga.Id, AttemptNumber: 3), TimeSpan.FromDays(daysUntilTermination));

        await repository.AddAsync(saga, cancellationToken);

        // Enqueue immediate onboarding email
        PaymentMethodMissingJob.Enqueue(new(saga.Id, AttemptNumber: 0));

        logger.LogInformation(
            "Started payment method missing saga {SagaId} for customer {CustomerId}",
            saga.Id,
            saga.CustomerId);

        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.SagaStarted, null, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningScheduled, saga.FirstWarningJobId, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningScheduled, saga.SecondWarningJobId, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.TerminationScheduled, saga.TerminationJobId, cancellationToken);

        return saga;
    }

    public async Task CancelSagaAsync(
        string customerId,
        PaymentMethodMissingSagaCancellationReason reason,
        CancellationToken cancellationToken = default)
    {
        var saga = await repository.GetActiveSagaForCustomerAsync(customerId, cancellationToken);
        if (saga == null)
        {
            logger.LogInformation(
                "No active saga found for customer {CustomerId}, nothing to cancel",
                customerId);
            return;
        }

        // Delete pending Hangfire jobs
        if (!string.IsNullOrEmpty(saga.FirstWarningJobId))
            BackgroundJob.Delete(saga.FirstWarningJobId);

        if (!string.IsNullOrEmpty(saga.SecondWarningJobId))
            BackgroundJob.Delete(saga.SecondWarningJobId);

        if (!string.IsNullOrEmpty(saga.TerminationJobId))
            BackgroundJob.Delete(saga.TerminationJobId);

        saga.Status = PaymentMethodMissingSagaStatus.Cancelled;
        saga.CancelledAtUtc = DateTime.UtcNow;
        saga.CancellationReason = reason;

        await ((Database.Repositories.IGenericRepository<PaymentMethodMissingSaga, PaymentMethodMissingSagaId, Data.StripeDbContext>)repository).UpdateAsync(saga, cancellationToken);

        logger.LogInformation(
            "Cancelled saga {SagaId} for customer {CustomerId}, reason: {Reason}",
            saga.Id,
            saga.CustomerId,
            reason);

        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.SagaCancelled, reason.ToString(), cancellationToken);

        if (reason == PaymentMethodMissingSagaCancellationReason.PaymentMethodAdded)
        {
            await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.PaymentMethodAddedDuringSaga, null, cancellationToken);
        }
    }

    public Task<PaymentMethodMissingSaga?> GetActiveSagaForCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        return repository.GetActiveSagaForCustomerAsync(customerId, cancellationToken);
    }

    private async Task LogAuditEventAsync(
        PaymentMethodMissingSagaId sagaId,
        PaymentMethodMissingSagaEvent eventType,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new PaymentMethodMissingSagaAuditLog
        {
            SagaId = sagaId,
            Event = eventType,
            EventTimestampUtc = DateTime.UtcNow,
            AdditionalData = additionalData
        };

        await repository.AddAuditLogAsync(auditLog, cancellationToken);
    }
}
