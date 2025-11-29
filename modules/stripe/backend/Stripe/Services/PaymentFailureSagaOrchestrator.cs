using Hangfire;
using Microsoft.Extensions.Logging;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;
using Stripe.Jobs.SagaJobs;

namespace Stripe.Services;

public interface IPaymentFailureSagaOrchestrator
{
    Task<PaymentFailureSaga?> StartSagaAsync(StripeInvoicePaymentFailedEvent @event, CancellationToken cancellationToken = default);
    Task UpdateSagaMetadataAsync(PaymentFailureSagaId sagaId, StripeInvoicePaymentFailedEvent @event, CancellationToken cancellationToken = default);
    Task CancelSagaAsync(string subscriptionId, PaymentFailureSagaCancellationReason reason, CancellationToken cancellationToken = default);
    Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
}

public class PaymentFailureSagaOrchestrator(
    IPaymentFailureSagaRepository repository,
    ILogger<PaymentFailureSagaOrchestrator> logger) : IPaymentFailureSagaOrchestrator
{ 
    public async Task<PaymentFailureSaga?> StartSagaAsync(
        StripeInvoicePaymentFailedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var existingSaga = await repository.GetActiveSagaForSubscriptionAsync(@event.SubscriptionId, cancellationToken);
        if (existingSaga != null)
        {
            logger.LogInformation(
                "Saga already exists for subscription {SubscriptionId}, skipping creation",
                @event.SubscriptionId);

            await LogAuditEventAsync(
                existingSaga.Id,
                PaymentFailureSagaEvent.DuplicateWebhookReceived,
                @event.InvoiceId,
                cancellationToken);

            return null;
        }

        var saga = new PaymentFailureSaga
        {
            SubscriptionId = @event.SubscriptionId,
            CustomerId = @event.CustomerId,
            InvoiceId = @event.InvoiceId,
            Status = PaymentFailureSagaStatus.Active,
            StartedAtUtc = DateTime.UtcNow,
            AmountDue = @event.AmountDue,
            Currency = @event.Currency,
            PaymentLink = @event.PaymentLink
        };

        // Schedule Hangfire delayed jobs
        PaymentFailedJob.Enqueue(new(saga.Id, AttemptNumber: 0));
        saga.FirstWarningJobId = PaymentFailedJob.Schedule(new(saga.Id, AttemptNumber: 1), TimeSpan.FromDays(1));
        saga.SecondWarningJobId = PaymentFailedJob.Schedule(new(saga.Id, AttemptNumber: 2), TimeSpan.FromDays(3));
        saga.TerminationJobId = PaymentFailedJob.Schedule(new(saga.Id, AttemptNumber: 3), TimeSpan.FromDays(5));

        await repository.AddAsync(saga, cancellationToken);

        logger.LogInformation(
            "Started payment failure saga {SagaId} for subscription {SubscriptionId}",
            saga.Id,
            saga.SubscriptionId);

        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.SagaStarted, saga.FirstWarningJobId, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.FirstWarningScheduled, saga.FirstWarningJobId, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.SecondWarningScheduled, saga.SecondWarningJobId, cancellationToken);
        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.TerminationScheduled, saga.TerminationJobId, cancellationToken);

        return saga;
    }

    public async Task UpdateSagaMetadataAsync(
        PaymentFailureSagaId sagaId,
        StripeInvoicePaymentFailedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var saga = await repository.GetByIdAsync(sagaId, cancellationToken);
        if (saga == null)
        {
            logger.LogWarning("Saga {SagaId} not found for metadata update", sagaId);
            return;
        }

        saga.InvoiceId = @event.InvoiceId;
        saga.AmountDue = @event.AmountDue;
        saga.PaymentLink = @event.PaymentLink;

        await repository.UpdateAsync(saga, cancellationToken);

        logger.LogInformation(
            "Updated metadata for saga {SagaId} with invoice {InvoiceId}",
            sagaId,
            @event.InvoiceId);
    }

    public async Task CancelSagaAsync(
        string subscriptionId,
        PaymentFailureSagaCancellationReason reason,
        CancellationToken cancellationToken = default)
    {
        var saga = await repository.GetActiveSagaForSubscriptionAsync(subscriptionId, cancellationToken);
        if (saga == null)
        {
            logger.LogInformation(
                "No active saga found for subscription {SubscriptionId}, nothing to cancel",
                subscriptionId);
            return;
        }

        // Delete pending Hangfire jobs
        if (!string.IsNullOrEmpty(saga.FirstWarningJobId))
            BackgroundJob.Delete(saga.FirstWarningJobId);

        if (!string.IsNullOrEmpty(saga.SecondWarningJobId))
            BackgroundJob.Delete(saga.SecondWarningJobId);

        if (!string.IsNullOrEmpty(saga.TerminationJobId))
            BackgroundJob.Delete(saga.TerminationJobId);

        saga.Status = PaymentFailureSagaStatus.Cancelled;
        saga.CancelledAtUtc = DateTime.UtcNow;
        saga.CancellationReason = reason;

        await repository.UpdateAsync(saga, cancellationToken);

        logger.LogInformation(
            "Cancelled saga {SagaId} for subscription {SubscriptionId}, reason: {Reason}",
            saga.Id,
            saga.SubscriptionId,
            reason);

        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.SagaCancelled, reason.ToString(), cancellationToken);

        if (reason == PaymentFailureSagaCancellationReason.PaymentSucceeded)
        {
            await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.PaymentSucceededDuringSaga, null, cancellationToken);
        }
    }

    public Task<PaymentFailureSaga?> GetActiveSagaForSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        return repository.GetActiveSagaForSubscriptionAsync(subscriptionId, cancellationToken);
    }

    private async Task LogAuditEventAsync(
        PaymentFailureSagaId sagaId,
        PaymentFailureSagaEvent eventType,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new PaymentFailureSagaAuditLog
        {
            SagaId = sagaId,
            Event = eventType,
            EventTimestampUtc = DateTime.UtcNow,
            AdditionalData = additionalData
        };

        await repository.AddAuditLogAsync(auditLog, cancellationToken);
    }
}
