using Core.Abstractions;
using Database.Models;
using Database.Repositories;
using Jobs;
using Microsoft.Extensions.Logging;
using Stripe.Data;

namespace Stripe.Jobs.SagaJobs;

public interface ISagaRepository<TSaga, TSagaId, TAuditLog>
    where TSaga : BaseEntityWithId<TSagaId>
    where TSagaId : IdBase<TSagaId>, IId<TSagaId>
{
    Task<TSaga?> GetByIdAsync(TSagaId id, CancellationToken cancellationToken = default);
    Task<TSaga> UpdateAsync(TSaga entity, CancellationToken cancellationToken = default);
    Task<TAuditLog> AddAuditLogAsync(TAuditLog auditLog, CancellationToken cancellationToken = default);
}

public abstract class BaseSagaJob<TJob, TSaga, TSagaId, TSagaStatus, TSagaEvent, TAuditLog, TRepository>(
    TRepository repository,
    ILogger<TJob> logger,
    string sagaTypeName) : JobBase<TJob, SagaJobArgs>
    where TJob : BaseSagaJob<TJob, TSaga, TSagaId, TSagaStatus, TSagaEvent, TAuditLog, TRepository>
    where TSaga : BaseEntityWithId<TSagaId>
    where TSagaId : IdBase<TSagaId>, IId<TSagaId>
    where TSagaStatus : Enum
    where TSagaEvent : Enum
    where TRepository : IGenericRepository<TSaga, TSagaId, StripeDbContext>, ISagaRepository<TSaga, TSagaId, TAuditLog>
{
    protected readonly TRepository Repository = repository;
    protected readonly ILogger<TJob> Logger = logger;

    public override string? GetDistributedLockKey(SagaJobArgs args)
        => $"{sagaTypeName}-saga:{args}";

    public override async Task ExecuteAsync(SagaJobArgs args)
    {
        Logger.LogInformation("Executing {SagaType} saga job for saga {SagaId}", sagaTypeName, args);

        // Cast object to TSagaId
        if (args.SagaId is not TSagaId sagaId)
        {
            Logger.LogError("Invalid saga ID type. Expected {ExpectedType}, got {ActualType}",
                typeof(TSagaId).Name,
                args.SagaId?.GetType().Name ?? "null");
            return;
        }

        var saga = await Repository.GetByIdAsync(sagaId, CancellationToken.None);

        if (saga == null)
        {
            Logger.LogWarning("{SagaType} saga {SagaId} not found, skipping execution", sagaTypeName, args);
            return;
        }

        if (!IsSagaActive(saga))
        {
            Logger.LogInformation(
                "{SagaType} saga {SagaId} is not active, skipping execution",
                sagaTypeName,
                args);
            return;
        }

        await ExecuteSagaStepAsync(saga, args.AttemptNumber, CancellationToken.None);

        await ((IGenericRepository<TSaga, TSagaId, StripeDbContext>)Repository).UpdateAsync(saga, CancellationToken.None);

        Logger.LogInformation("Completed {SagaType} saga job for saga {SagaId}", sagaTypeName, args);
    }

    protected abstract bool IsSagaActive(TSaga saga);
    protected abstract Task ExecuteSagaStepAsync(TSaga saga, int attemptNumber, CancellationToken cancellationToken);
    protected abstract TAuditLog CreateAuditLog(TSagaId sagaId, TSagaEvent eventType, string? additionalData);

    protected async Task LogAuditEventAsync(
        TSagaId sagaId,
        TSagaEvent eventType,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = CreateAuditLog(sagaId, eventType, additionalData);
        await Repository.AddAuditLogAsync(auditLog, cancellationToken);
    }
}
