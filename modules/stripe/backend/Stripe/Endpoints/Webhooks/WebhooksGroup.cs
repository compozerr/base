using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.Webhooks.ProcessWebhook;

namespace Stripe.Endpoints.Webhooks;

public static class WebhooksGroup
{
    public const string Route = "webhooks";

    public static RouteGroupBuilder AddWebhooksGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddProcessWebhookRoute();

        return group;
    }
}
