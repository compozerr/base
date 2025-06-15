using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.PaymentMethods.RemovePaymentMethod;

public static class RemovePaymentMethodRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddRemovePaymentMethodRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static async Task<RemovePaymentMethodResponse> ExecuteAsync(
        string paymentMethodId,
        IMediator mediator)
        => await mediator.Send(new RemovePaymentMethodCommand(paymentMethodId));
}
