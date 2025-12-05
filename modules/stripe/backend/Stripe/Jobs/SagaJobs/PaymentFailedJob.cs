using Auth.Abstractions;
using Auth.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe.Abstractions;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;
using Stripe.Services;

namespace Stripe.Jobs.SagaJobs;

public class PaymentFailedJob(
    IPaymentFailureSagaRepository repository,
    ILogger<PaymentFailedJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IPublisher publisher,
    IStripeCustomerValidationService stripeCustomerValidationService) : BaseSagaJob<
        PaymentFailedJob,
        PaymentFailureSaga,
        PaymentFailureSagaId,
        PaymentFailureSagaStatus,
        PaymentFailureSagaEvent,
        PaymentFailureSagaAuditLog,
        IPaymentFailureSagaRepository>(repository, logger, "payment-failure")
{
    protected override bool IsSagaActive(PaymentFailureSaga saga)
        => saga.Status == PaymentFailureSagaStatus.Active;

    protected override PaymentFailureSagaAuditLog CreateAuditLog(
        PaymentFailureSagaId sagaId,
        PaymentFailureSagaEvent eventType,
        string? additionalData)
    {
        return new PaymentFailureSagaAuditLog
        {
            SagaId = sagaId,
            Event = eventType,
            EventTimestampUtc = DateTime.UtcNow,
            AdditionalData = additionalData
        };
    }

    protected override async Task ExecuteSagaStepAsync(
        PaymentFailureSaga saga,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing payment failed warning attempt {AttemptNumber} for saga {SagaId}", attemptNumber, saga.Id);

        var userId = await stripeCustomerRepository.GetInternalIdByStripeCustomerIdAsync(
            saga.CustomerId,
            cancellationToken);

        if (!UserId.TryParse(userId, out var parsedUserId) || userId == null)
        {
            Logger.LogError(
                "No internal user id found for Stripe Customer ID: {CustomerId} in saga {SagaId}",
                saga.CustomerId,
                saga.Id);
            return;
        }

        var user = await userRepository.GetByIdAsync(parsedUserId, cancellationToken);
        if (user == null)
        {
            Logger.LogError(
                "User with ID: {UserId} not found for saga {SagaId}",
                parsedUserId,
                saga.Id);
            return;
        }

        // Check if the invoice has been paid in the meantime
        var isInvoicePaid = await stripeCustomerValidationService.IsInvoicePaidAsync(
            saga.InvoiceId,
            cancellationToken);

        if (isInvoicePaid)
        {
            Logger.LogInformation(
                "Invoice {InvoiceId} has been paid. Marking saga {SagaId} as resolved without sending warning.",
                saga.InvoiceId,
                saga.Id);

            saga.Status = PaymentFailureSagaStatus.Completed;
            saga.CompletedAtUtc = DateTime.UtcNow;
            await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.PaymentSucceededDuringSaga, "Invoice was paid before warning could be sent", cancellationToken);
            return;
        }

        // Check if the customer has added a default payment method in the meantime
        var hasPaymentMethod = await stripeCustomerValidationService.HasDefaultPaymentMethodAsync(
            saga.CustomerId,
            cancellationToken);

        if (hasPaymentMethod)
        {
            Logger.LogInformation(
                "Customer {CustomerId} now has a payment method for saga {SagaId}. Payment may be retried automatically by Stripe.",
                saga.CustomerId,
                saga.Id);
            // Note: We still send the event because having a payment method doesn't guarantee the invoice will be paid
            // Stripe's retry logic will handle the payment attempt
        }

        var @event = new PaymentFailedEvent(
            saga.InvoiceId,
            saga.CustomerId,
            saga.SubscriptionId,
            saga.AmountDue,
            saga.Currency,
            saga.StartedAtUtc,
            (int)(DateTime.UtcNow - saga.StartedAtUtc).TotalDays,
            saga.PaymentLink,
            attemptNumber);

        await publisher.Publish(@event, cancellationToken);

        // Update saga timestamps based on attempt number
        switch (attemptNumber)
        {
            case 1:
                saga.FirstWarningSentAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.FirstWarningSent, null, cancellationToken);
                break;
            case 2:
                saga.SecondWarningSentAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.SecondWarningSent, null, cancellationToken);
                break;
            case 3:
                saga.TerminationExecutedAtUtc = DateTime.UtcNow;
                saga.Status = PaymentFailureSagaStatus.Completed;
                saga.CompletedAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.TerminationExecuted, null, cancellationToken);
                break;
            default:
                await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.OnboardingSent, null, cancellationToken);
                break;
        }

        Logger.LogInformation("Payment failed warning attempt {AttemptNumber} sent for saga {SagaId}", attemptNumber, saga.Id);
    }
}
