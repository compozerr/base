using Core.MediatR;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public sealed record GetUserSubscriptionsCommand(
    string UserId) : ICommand<GetUserSubscriptionsResponse>;
