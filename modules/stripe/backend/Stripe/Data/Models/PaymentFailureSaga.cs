using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;

public class PaymentFailureSaga : BaseEntityWithId<PaymentFailureSagaId>
{
    /// <summary>
    /// The Stripe subscription ID associated with this saga.
    /// </summary>
    public required string SubscriptionId { get; set; }

    /// <summary>
    /// The Stripe customer ID.
    /// </summary>
    public required string CustomerId { get; set; }

    /// <summary>
    /// The Stripe invoice ID that failed payment.
    /// </summary>
    public required string InvoiceId { get; set; }

    /// <summary>
    /// Current status of the saga.
    /// </summary>
    public PaymentFailureSagaStatus Status { get; set; }

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
    public PaymentFailureSagaCancellationReason? CancellationReason { get; set; }

    /// <summary>
    /// Amount due on the failed invoice.
    /// </summary>
    public decimal AmountDue { get; set; }

    /// <summary>
    /// Currency of the invoice (e.g., "usd", "eur").
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Stripe hosted payment link for the invoice.
    /// </summary>
    public required string PaymentLink { get; set; }

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
    /// When the project termination was executed (UTC).
    /// </summary>
    public DateTime? TerminationExecutedAtUtc { get; set; }
}

public enum PaymentFailureSagaStatus
{
    Active = 0,
    Completed = 1,
    Cancelled = 2
}

public enum PaymentFailureSagaCancellationReason
{
    PaymentSucceeded = 0,
    SubscriptionCancelled = 1,
    ManualCancellation = 2
}
