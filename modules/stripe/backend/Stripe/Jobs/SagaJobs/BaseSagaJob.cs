using Jobs;
using Microsoft.Extensions.Logging;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Data.Repositories;

namespace Stripe.Jobs.SagaJobs;

public abstract class BaseSagaJob<T>(
    IPaymentFailureSagaRepository repository,
    ILogger<T> logger) : JobBase<T, PaymentFailureSagaId> where T : BaseSagaJob<T>
{
    protected readonly IPaymentFailureSagaRepository Repository = repository;
    protected readonly ILogger<T> Logger = logger;

    public override string? GetDistributedLockKey(PaymentFailureSagaId sagaId)
        => $"payment-failure-saga:{sagaId}";

    public override async Task ExecuteAsync(PaymentFailureSagaId sagaId)
    {
        Logger.LogInformation("Executing saga job for saga {SagaId}", sagaId);

        var saga = await Repository.GetByIdAsync(sagaId, CancellationToken.None);

        if (saga == null)
        {
            Logger.LogWarning("Saga {SagaId} not found, skipping execution", sagaId);
            return;
        }

        if (saga.Status != PaymentFailureSagaStatus.Active)
        {
            Logger.LogInformation(
                "Saga {SagaId} is not active (status: {Status}), skipping execution",
                sagaId,
                saga.Status);
            return;
        }

        await ExecuteSagaStepAsync(saga, CancellationToken.None);

        await Repository.UpdateAsync(saga, CancellationToken.None);

        Logger.LogInformation("Completed saga job for saga {SagaId}", sagaId);
    }

    protected abstract Task ExecuteSagaStepAsync(PaymentFailureSaga saga, CancellationToken cancellationToken);

    protected async Task LogAuditEventAsync(
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

        await Repository.AddAuditLogAsync(auditLog, cancellationToken);
    }
}
