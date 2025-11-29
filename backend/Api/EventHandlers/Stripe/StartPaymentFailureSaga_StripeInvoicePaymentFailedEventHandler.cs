using Core.Abstractions;
using Stripe.Events;
using Stripe.Services;

namespace Api.EventHandlers.Stripe;

public class StartPaymentFailureSaga_StripeInvoicePaymentFailedEventHandler(
    IPaymentFailureSagaOrchestrator orchestrator) : IEventHandler<StripeInvoicePaymentFailedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<StartPaymentFailureSaga_StripeInvoicePaymentFailedEventHandler>();

    public async Task Handle(StripeInvoicePaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Information(
            "Processing payment failure for invoice {InvoiceId}, subscription {SubscriptionId}",
            notification.InvoiceId,
            notification.SubscriptionId);

        var existingSaga = await orchestrator.GetActiveSagaForSubscriptionAsync(
            notification.SubscriptionId,
            cancellationToken);

        if (existingSaga != null)
        {
            _logger.Information(
                "Active saga {SagaId} already exists for subscription {SubscriptionId}, updating metadata",
                existingSaga.Id,
                notification.SubscriptionId);

            await orchestrator.UpdateSagaMetadataAsync(existingSaga.Id, notification, cancellationToken);
        }
        else
        {
            _logger.Information(
                "Starting new payment failure saga for subscription {SubscriptionId}",
                notification.SubscriptionId);

            await orchestrator.StartSagaAsync(notification, cancellationToken);
        }
    }
}
