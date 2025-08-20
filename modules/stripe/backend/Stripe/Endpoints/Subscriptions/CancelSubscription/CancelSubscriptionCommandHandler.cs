using Core.MediatR;

namespace Stripe.Endpoints.Subscriptions.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler : ICommandHandler<CancelSubscriptionCommand, CancelSubscriptionResponse>
{
    public async Task<CancelSubscriptionResponse> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var subscriptionService = new Stripe.SubscriptionService();

        // Cancel the subscription
        var options = new Stripe.SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = !command.CancelImmediately // If not immediate, cancel at period end
        };

        if (command.CancelImmediately)
        {
            // For immediate cancellation, we need to use Cancel endpoint
            var cancelledSubscription = await subscriptionService.CancelAsync(
                command.SubscriptionId,
                new Stripe.SubscriptionCancelOptions()
                {
                    
                },
                null,
                cancellationToken);

            return new CancelSubscriptionResponse(
                cancelledSubscription.Id,
                cancelledSubscription.Status,
                cancelledSubscription.CanceledAt?.ToString("yyyy-MM-dd"),
                true);
        }
        else
        {
            // For cancellation at period end, we update the subscription
            var updatedSubscription = await subscriptionService.UpdateAsync(
                command.SubscriptionId,
                options,
                null,
                cancellationToken);

            return new CancelSubscriptionResponse(
                updatedSubscription.Id,
                updatedSubscription.Status,
                updatedSubscription.CanceledAt?.ToString("yyyy-MM-dd"),
                false);
        }
    }
}
