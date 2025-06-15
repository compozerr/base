using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using System.Threading.Tasks;

namespace Stripe.Endpoints.PaymentMethods.SetDefaultPaymentMethod;

public static class SetDefaultPaymentMethodRoute
{
    public const string Route = "default";

    public static RouteHandlerBuilder AddSetDefaultPaymentMethodRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<SetDefaultPaymentMethodResponse> ExecuteAsync(
        SetDefaultPaymentMethodRequest request,
        IMediator mediator)
        => await mediator.Send(new SetDefaultPaymentMethodCommand(request.PaymentMethodId));
}

public record SetDefaultPaymentMethodRequest(string PaymentMethodId);
