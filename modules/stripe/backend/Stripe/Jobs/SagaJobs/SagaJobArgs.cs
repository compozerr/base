using Stripe.Abstractions;

namespace Stripe.Jobs.SagaJobs;

public sealed record SagaJobArgs(
    PaymentFailureSagaId SagaId,
    int AttemptNumber)
{
    public override string ToString()
    {
        return $"sagaId={SagaId};attemptNumber={AttemptNumber}";
    }
};
