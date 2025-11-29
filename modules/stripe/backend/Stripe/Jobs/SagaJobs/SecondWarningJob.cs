using Api.EventHandlers.Stripe.PaymentFailedActions;
using Auth.Abstractions;
using Auth.Repositories;
using Microsoft.Extensions.Logging;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Stripe.Jobs.SagaJobs;

public class SecondWarningJob(
    IPaymentFailureSagaRepository repository,
    ILogger<SecondWarningJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IServiceProvider serviceProvider) : BaseSagaJob<SecondWarningJob>(repository, logger)
{
    protected override async Task ExecuteSagaStepAsync(PaymentFailureSaga saga, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing second warning for saga {SagaId}", saga.Id);

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

        var @event = new StripeInvoicePaymentFailedEvent(
            saga.InvoiceId,
            saga.CustomerId,
            saga.SubscriptionId,
            saga.AmountDue,
            saga.Currency,
            saga.StartedAtUtc,
            (int)(DateTime.UtcNow - saga.StartedAtUtc).TotalDays,
            saga.PaymentLink,
            2,
            null,
            null);

        var action = new SecondWarningMail_PaymentFailedAction(@event, user, serviceProvider);
        await action.ExecuteAsync(cancellationToken);

        saga.SecondWarningSentAtUtc = DateTime.UtcNow;

        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.SecondWarningSent, null, cancellationToken);

        Logger.LogInformation("Second warning sent for saga {SagaId}", saga.Id);
    }
}
