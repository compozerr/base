using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.PaymentMethods.CreateSetupIntent;

public static class CreateSetupIntentRoute
{
    public const string Route = "setup-intent";

    public static RouteHandlerBuilder AddCreateSetupIntentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<CreateSetupIntentResponse> ExecuteAsync(IMediator mediator)
        => await mediator.Send(new CreateSetupIntentCommand());
}
