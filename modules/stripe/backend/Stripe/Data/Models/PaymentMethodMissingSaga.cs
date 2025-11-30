using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;

public class PaymentMethodMissingSaga : BaseEntityWithId<PaymentMethodMissingSagaId>
{
    /// <summary>
    /// The Stripe customer ID.
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// Current status of the saga.
    /// </summary>
    public PaymentMethodMissingSagaStatus Status { get; set; }

    /// <summary>
    /// When the saga started (UTC).
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// When the saga completed successfully (UTC).
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// When the saga was cancelled (UTC).
    /// </summary>
    public DateTime? CancelledAtUtc { get; set; }

    /// <summary>
    /// Reason the saga was cancelled.
    /// </summary>
    public PaymentMethodMissingSagaCancellationReason? CancellationReason { get; set; }

    /// <summary>
    /// Hangfire job ID for the first warning email (for cancellation).
    /// </summary>
    public string? FirstWarningJobId { get; set; }

    /// <summary>
    /// Hangfire job ID for the second warning email (for cancellation).
    /// </summary>
    public string? SecondWarningJobId { get; set; }

    /// <summary>
    /// Hangfire job ID for the termination job (for cancellation).
    /// </summary>
    public string? TerminationJobId { get; set; }

    /// <summary>
    /// When the first warning email was sent (UTC).
    /// </summary>
    public DateTime? FirstWarningSentAtUtc { get; set; }

    /// <summary>
    /// When the second warning email was sent (UTC).
    /// </summary>
    public DateTime? SecondWarningSentAtUtc { get; set; }

    /// <summary>
    /// When the all projects termination was executed (UTC).
    /// </summary>
    public DateTime? TerminationExecutedAtUtc { get; set; }
}

public enum PaymentMethodMissingSagaStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public enum PaymentMethodMissingSagaCancellationReason
{
    PaymentMethodAdded = 0,
    ManualCancellation = 1
}
