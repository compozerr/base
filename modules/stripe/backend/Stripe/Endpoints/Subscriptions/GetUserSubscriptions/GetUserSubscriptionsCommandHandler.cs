using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public class GetUserSubscriptionsCommandHandler(ISubscriptionsService subscriptionService) : ICommandHandler<GetUserSubscriptionsCommand, GetUserSubscriptionsResponse>
{

    public async Task<GetUserSubscriptionsResponse> Handle(
        GetUserSubscriptionsCommand request,
        CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionService.GetSubscriptionsForUserAsync(
            cancellationToken);

        return new GetUserSubscriptionsResponse(subscriptions);
    }
}
