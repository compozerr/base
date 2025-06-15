using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

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
