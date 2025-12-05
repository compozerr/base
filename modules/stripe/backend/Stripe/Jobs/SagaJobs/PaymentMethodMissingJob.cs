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

public class PaymentMethodMissingJob(
    IPaymentMethodMissingSagaRepository repository,
    ILogger<PaymentMethodMissingJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IPublisher publisher,
    IStripeCustomerValidationService stripeCustomerValidationService) : BaseSagaJob<
        PaymentMethodMissingJob,
        PaymentMethodMissingSaga,
        PaymentMethodMissingSagaId,
        PaymentMethodMissingSagaStatus,
        PaymentMethodMissingSagaEvent,
        PaymentMethodMissingSagaAuditLog,
        IPaymentMethodMissingSagaRepository>(repository, logger, "payment-method-missing")
{
    protected override bool IsSagaActive(PaymentMethodMissingSaga saga)
        => saga.Status == PaymentMethodMissingSagaStatus.Active;

    protected override PaymentMethodMissingSagaAuditLog CreateAuditLog(
        PaymentMethodMissingSagaId sagaId,
        PaymentMethodMissingSagaEvent eventType,
        string? additionalData)
    {
        return new PaymentMethodMissingSagaAuditLog
        {
            SagaId = sagaId,
            Event = eventType,
            EventTimestampUtc = DateTime.UtcNow,
            AdditionalData = additionalData
        };
    }

    protected override async Task ExecuteSagaStepAsync(
        PaymentMethodMissingSaga saga,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing payment method missing warning attempt {AttemptNumber} for saga {SagaId}", attemptNumber, saga.Id);

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

        // Check if the customer has added a payment method since the saga started
        var hasPaymentMethod = await stripeCustomerValidationService.HasDefaultPaymentMethodAsync(
            saga.CustomerId,
            cancellationToken);

        if (hasPaymentMethod)
        {
            Logger.LogInformation(
                "Customer {CustomerId} now has a payment method. Marking saga {SagaId} as resolved without sending warning.",
                saga.CustomerId,
                saga.Id);

            saga.Status = PaymentMethodMissingSagaStatus.Completed;
            saga.CompletedAtUtc = DateTime.UtcNow;
            await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.PaymentMethodAddedDuringSaga, "Payment method added before warning could be sent", cancellationToken);
            return;
        }

        var @event = new PaymentMethodMissingEvent(
            saga.CustomerId,
            attemptNumber,
            saga);

        await publisher.Publish(@event, cancellationToken);

        // Update saga timestamps based on attempt number
        switch (attemptNumber)
        {
            case 1:
                saga.FirstWarningSentAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningSent, null, cancellationToken);
                break;
            case 2:
                saga.SecondWarningSentAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningSent, null, cancellationToken);
                break;
            case 3:
                saga.TerminationExecutedAtUtc = DateTime.UtcNow;
                saga.Status = PaymentMethodMissingSagaStatus.Completed;
                saga.CompletedAtUtc = DateTime.UtcNow;
                await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.TerminationExecuted, null, cancellationToken);
                break;
            default:
                await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningSent, null, cancellationToken);
                break;
        }

        Logger.LogInformation("Payment method missing warning attempt {AttemptNumber} sent for saga {SagaId}", attemptNumber, saga.Id);
    }
}
