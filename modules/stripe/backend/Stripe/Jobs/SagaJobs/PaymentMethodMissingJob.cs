using Auth.Abstractions;
using Auth.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Stripe.Jobs.SagaJobs;

public class PaymentMethodMissingJob(
    IPaymentMethodMissingSagaRepository repository,
    ILogger<PaymentMethodMissingJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IPublisher publisher) : BaseSagaJob<PaymentMethodMissingJob>(repository, logger)
{
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

        var @event = new PaymentMethodMissingEvent(
            saga.CustomerId,
            attemptNumber);

        await publisher.Publish(@event, cancellationToken);

        await LogAuditEventAsync(saga.Id, PaymentMethodMissingSagaEvent.WarningSent, null, cancellationToken);

        Logger.LogInformation("Payment method missing warning attempt {AttemptNumber} sent for saga {SagaId}", attemptNumber, saga.Id);
    }
}
