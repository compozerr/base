using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;

public class PaymentFailureSagaAuditLog : BaseEntityWithId<PaymentFailureSagaAuditLogId>
{
    /// <summary>
    /// The saga this audit log entry belongs to.
    /// </summary>
    public required PaymentFailureSagaId SagaId { get; set; }

    /// <summary>
    /// The type of event that occurred.
    /// </summary>
    public required PaymentFailureSagaEvent Event { get; set; }

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime EventTimestampUtc { get; set; }

    /// <summary>
    /// Additional metadata about the event (JSON format).
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Hangfire job ID associated with this event (if applicable).
    /// </summary>
    public string? JobId { get; set; }
}

public enum PaymentFailureSagaEvent
{
    SagaStarted = 0,
    FirstWarningScheduled = 1,
    FirstWarningSent = 2,
    SecondWarningScheduled = 3,
    SecondWarningSent = 4,
    TerminationScheduled = 5,
    TerminationExecuted = 6,
    SagaCancelled = 7,
    DuplicateWebhookReceived = 8,
    PaymentSucceededDuringSaga = 9
}
