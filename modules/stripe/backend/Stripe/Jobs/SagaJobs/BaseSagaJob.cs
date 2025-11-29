using Jobs;
using Microsoft.Extensions.Logging;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Data.Repositories;

namespace Stripe.Jobs.SagaJobs;

public abstract class BaseSagaJob<T>(
    IPaymentFailureSagaRepository repository,
    ILogger<T> logger) : JobBase<T, SagaJobArgs> where T : BaseSagaJob<T>
{
    protected readonly IPaymentFailureSagaRepository Repository = repository;
    protected readonly ILogger<T> Logger = logger;

    public override string? GetDistributedLockKey(SagaJobArgs args)
        => $"payment-failure-saga:{args}";

    public override async Task ExecuteAsync(SagaJobArgs args)
    {
        Logger.LogInformation("Executing saga job for saga {SagaId}", args);

        var saga = await Repository.GetByIdAsync(args.SagaId, CancellationToken.None);

        if (saga == null)
        {
            Logger.LogWarning("Saga {SagaId} not found, skipping execution", args);
            return;
        }

        if (saga.Status != PaymentFailureSagaStatus.Active)
        {
            Logger.LogInformation(
                "Saga {SagaId} is not active (status: {Status}), skipping execution",
                args,
                saga.Status);
            return;
        }

        await ExecuteSagaStepAsync(saga, args.AttemptNumber, CancellationToken.None);

        await Repository.UpdateAsync(saga, CancellationToken.None);

        Logger.LogInformation("Completed saga job for saga {SagaId}", args);
    }

    protected abstract Task ExecuteSagaStepAsync(PaymentFailureSaga saga, int attemptNumber, CancellationToken cancellationToken);

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
