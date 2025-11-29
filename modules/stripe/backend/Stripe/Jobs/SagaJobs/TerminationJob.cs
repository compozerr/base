using Api.EventHandlers.Stripe.PaymentFailedActions;
using Auth.Abstractions;
using Auth.Repositories;
using Microsoft.Extensions.Logging;
using Stripe.Data.Models;
using Stripe.Data.Repositories;
using Stripe.Events;

namespace Stripe.Jobs.SagaJobs;

public class TerminationJob(
    IPaymentFailureSagaRepository repository,
    ILogger<TerminationJob> logger,
    IStripeCustomerRepository stripeCustomerRepository,
    IUserRepository userRepository,
    IServiceProvider serviceProvider) : BaseSagaJob<TerminationJob>(repository, logger)
{
    protected override async Task ExecuteSagaStepAsync(PaymentFailureSaga saga, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Executing termination for saga {SagaId}", saga.Id);

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
            3,
            null,
            null);

        // Execute termination email
        var terminationMailAction = new ThirdAndTerminationMail_PaymentFailedAction(@event, user, serviceProvider);
        await terminationMailAction.ExecuteAsync(cancellationToken);

        // Execute project termination
        var terminateProjectAction = new TerminateProject_PaymentFailedAction(@event, user, serviceProvider);
        await terminateProjectAction.ExecuteAsync(cancellationToken);

        // Mark saga as completed
        saga.Status = PaymentFailureSagaStatus.Completed;
        saga.CompletedAtUtc = DateTime.UtcNow;
        saga.TerminationExecutedAtUtc = DateTime.UtcNow;

        await LogAuditEventAsync(saga.Id, PaymentFailureSagaEvent.TerminationExecuted, null, cancellationToken);

        Logger.LogInformation("Termination completed for saga {SagaId}", saga.Id);
    }
}
