using Core.MediatR;
using MediatR;
using Stripe.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public class GetUserSubscriptionsCommandHandler : ICommandHandler<GetUserSubscriptionsCommand, GetUserSubscriptionsResponse>
{
    private readonly IStripeService _stripeService;

    public GetUserSubscriptionsCommandHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    public async Task<GetUserSubscriptionsResponse> Handle(
        GetUserSubscriptionsCommand request, 
        CancellationToken cancellationToken)
    {
        var subscriptions = await _stripeService.GetSubscriptionsForUserAsync(
            request.UserId, 
            cancellationToken);

        return new GetUserSubscriptionsResponse(subscriptions);
    }
}
