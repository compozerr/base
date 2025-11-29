using Auth.Abstractions;
using Auth.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Stripe.Jobs.SagaJobs;

public class PaymentFailedJob(
    IPaymentFailureSagaRepository repository,
    ILogger<PaymentFailedJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IPublisher publisher) : BaseSagaJob<PaymentFailedJob>(repository, logger)
{
    protected override async Task ExecuteSagaStepAsync(
        PaymentFailureSaga saga,
        int attemptNumber,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing first warning for saga {SagaId}", saga.Id);

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

        var @event = new PaymentFailedEvent(
            saga.InvoiceId,
            saga.CustomerId,
            saga.SubscriptionId,
            saga.AmountDue,
            saga.Currency,
            saga.StartedAtUtc,
            (int)(DateTime.UtcNow - saga.StartedAtUtc).TotalDays,
            saga.PaymentLink,
            attemptNumber,
            null,
            null);

        await publisher.Publish(@event, cancellationToken);

        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.FirstWarningSent, null, cancellationToken);

        Logger.LogInformation("First warning sent for saga {SagaId}", saga.Id);
    }
}
