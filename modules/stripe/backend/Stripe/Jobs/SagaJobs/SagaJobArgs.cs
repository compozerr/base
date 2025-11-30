using Core.Abstractions;

namespace Stripe.Jobs.SagaJobs;

public sealed record SagaJobArgs<TSagaId>(
    TSagaId SagaId,
    int AttemptNumber) where TSagaId : IdBase<TSagaId>, IId<TSagaId>, IParsable<TSagaId>, IComparable<TSagaId>
{
    public override string ToString()
    {
        return $"sagaId={SagaId};attemptNumber={AttemptNumber}";
    }
}

// Keep non-generic version for backward compatibility with existing orchestrator usage
public sealed record SagaJobArgs(
    object SagaId,
    int AttemptNumber)
{
    public override string ToString()
    {
        return $"sagaId={SagaId};attemptNumber={AttemptNumber}";
    }
}
