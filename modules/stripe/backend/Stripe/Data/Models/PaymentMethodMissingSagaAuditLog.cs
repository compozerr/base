using Database.Models;
using Stripe.Abstractions;

namespace Stripe.Data.Models;

public class PaymentMethodMissingSagaAuditLog : BaseEntityWithId<PaymentMethodMissingSagaAuditLogId>
{
    /// <summary>
    /// The saga this audit log entry belongs to.
    /// </summary>
    public required PaymentMethodMissingSagaId SagaId { get; set; }

    /// <summary>
    /// The type of event that occurred.
    /// </summary>
    public required PaymentMethodMissingSagaEvent Event { get; set; }

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

public enum PaymentMethodMissingSagaEvent
{
    SagaStarted = 0,
    WarningScheduled = 1,
    WarningSent = 2,
    TerminationScheduled = 3,
    TerminationExecuted = 4,
    SagaCancelled = 5,
    PaymentMethodAddedDuringSaga = 6
}
